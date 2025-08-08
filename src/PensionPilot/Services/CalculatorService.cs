using PensionPilot.Models.Config;

namespace PensionPilot.Services;

public class CalculatorService(ITaxService tax) : ICalculatorService
{
    public Task<IReadOnlyList<YearResult>> ProjectAsync(AppConfig cfg)
    {
        var list = new List<YearResult>();

        var age = cfg.Timeline.CurrentAge;

        decimal portfolio = cfg.Portfolio.CurrentValue;
        decimal studyFund = cfg.StudyFund.CurrentValue;
        decimal pensionBalance = cfg.Pension.CurrentBalance;
        decimal salary = cfg.Salary.AnnualGrossSalary;

        for (int year = 0; age <= cfg.Timeline.ModelEndAge; year++, age++)
        {
            var isWorking = age < cfg.Timeline.RetirementAge;
            var isPension = age >= cfg.Timeline.PensionAge;

            // Salary
            decimal salaryGross = isWorking ? salary : 0;
            decimal salaryTax = 0;
            decimal salaryNet = 0;
            decimal employeePensionContr = 0;
            decimal employerPensionContr = 0;
            decimal employeeStudyFundContr = 0;
            decimal employerStudyFundContr = 0;

            if (isWorking && salaryGross > 0)
            {
                // Income tax on gross
                var incomeTax = tax.CalculateIncomeTax(salaryGross, cfg.Taxes.IncomeTaxBrackets);
                // Social security on gross
                var socialTax = tax.CalculateIncomeTax(salaryGross, cfg.Taxes.SocialSecurityTaxBrackets);
                salaryTax = incomeTax + socialTax;

                // Calculate contributions
                employeePensionContr = salaryGross * cfg.Salary.EmployeePensionContributionRate;
                employerPensionContr = salaryGross * cfg.Salary.EmployerPensionContributionRate;

                // Study fund contributions only if active (age >= ActiveFromAge) and ActiveFromAge set
                if (cfg.StudyFund.ActiveFromAge is int activeFrom && age >= activeFrom)
                {
                    employeeStudyFundContr = salaryGross * cfg.Salary.EmployeeStudyFundContributionRate;
                    employerStudyFundContr = salaryGross * cfg.Salary.EmployerStudyFundContributionRate;
                    var totalStudyContr = employeeStudyFundContr + employerStudyFundContr;
                    if (cfg.StudyFund.MaxAnnualContribution is decimal cap && totalStudyContr > cap)
                    {
                        // Scale down proportionally to keep ratio employee/employer the same
                        var scale = cap / totalStudyContr;
                        employeeStudyFundContr *= scale;
                        employerStudyFundContr *= scale;
                    }
                }

                // Net salary after tax and employee contributions
                salaryNet = salaryGross - salaryTax - employeePensionContr - employeeStudyFundContr;
            }

            // RSU: annual grant, vest over 4 years equally, sell on vest
            decimal rsuVested = 0;
            decimal rsuTax = 0;
            decimal rsuNet = 0;
            if (cfg.Rsu.AnnualGrantAmount > 0)
            {
                // Assume grants every year; for year N, vest 1/4 of each of the last 4 grants
                for (int g = 0; g < cfg.Rsu.VestingYears; g++)
                {
                    var grantIndex = year - g; // grant in this or previous years
                    if (grantIndex < 0) continue;
                    rsuVested += cfg.Rsu.AnnualGrantAmount / cfg.Rsu.VestingYears;
                }
                // Tax RSU as income on vest
                rsuTax = tax.CalculateIncomeTax(rsuVested, cfg.Taxes.IncomeTaxBrackets);
                rsuNet = rsuVested - rsuTax;
            }

            // Pension growth until pension age, then payout as annuity
            decimal pensionPayoutGross = 0;
            decimal pensionTax = 0;
            decimal pensionPayoutNet = 0;
            if (!isPension)
            {
                pensionBalance *= 1 + cfg.Pension.AnnualReturnRateUntilPension;
                // Add ongoing contributions
                pensionBalance += employeePensionContr + employerPensionContr;
            }
            else
            {
                // Calculate monthly payout by annuity factor
                var monthly = pensionBalance / Math.Max(1, cfg.Pension.AnnuityFactor);
                pensionPayoutGross = monthly * 12;
                pensionTax = tax.CalculateIncomeTax(pensionPayoutGross, cfg.Taxes.IncomeTaxBrackets);
                pensionPayoutNet = pensionPayoutGross - pensionTax;
                // Reduce balance by payout (ignoring investment return post-annuitization for simplicity)
                pensionBalance = Math.Max(0, pensionBalance - pensionPayoutGross);
            }

            // Portfolio return first
            var portfolioStart = portfolio;
            var portfolioReturn = portfolio * cfg.Portfolio.AnnualReturnRate;
            portfolio += portfolioReturn;

            // Study fund return and add contributions
            var studyFundStart = studyFund;
            var studyFundReturn = studyFund * cfg.StudyFund.AnnualReturnRate;
            studyFund += studyFundReturn + employeeStudyFundContr + employerStudyFundContr;

            // Cashflows
            decimal expenses = YearlyExpenses(cfg, year);
            decimal incomeNet = salaryNet + rsuNet + pensionPayoutNet + cfg.AdditionalIncome.AnnualNetIncome;

            // Add RSU net and any net salary savings + additional income to portfolio by default when working
            portfolio += rsuNet;
            if (isWorking)
            {
                // treat additional net income like salaryNet for savings
                var savings = Math.Max(0, salaryNet + cfg.AdditionalIncome.AnnualNetIncome - expenses);
                portfolio += savings;
            }

            // If not working, cover expenses from income + withdrawals (portfolio first, then study fund as last resort)
            decimal portfolioWithdrawalGross = 0;
            decimal portfolioCapGainsTax = 0;
            decimal studyFundWithdrawal = 0;
            if (!isWorking)
            {
                var shortfall = Math.Max(0, expenses - incomeNet);

                // Between retirement and pension age, honor a withdrawal rate limit
                if (!isPension)
                {
                    var allowed = portfolioStart * cfg.Portfolio.PrePensionWithdrawalRate;
                    var fromPortfolio = Math.Min(shortfall, allowed);
                    portfolioWithdrawalGross = fromPortfolio;
                    portfolio -= fromPortfolio;
                    shortfall -= fromPortfolio;
                }
                else
                {
                    // In pension years, withdraw as needed
                    var fromPortfolio = Math.Min(shortfall, portfolio);
                    portfolioWithdrawalGross = fromPortfolio;
                    portfolio -= fromPortfolio;
                    shortfall -= fromPortfolio;
                }

                // Capital gains tax approximation: tax on withdrawals proportional to gains fraction
                if (cfg.Portfolio.TaxWithdrawalsAsCapitalGains && portfolioReturn > 0 && portfolioWithdrawalGross > 0)
                {
                    var gainsFraction = portfolioReturn / Math.Max(1, portfolioStart + portfolioReturn);
                    portfolioCapGainsTax = portfolioWithdrawalGross * gainsFraction * cfg.Taxes.CapitalGainsRate;
                    portfolio -= portfolioCapGainsTax;
                }

                if (shortfall > 0)
                {
                    studyFundWithdrawal = Math.Min(shortfall, studyFund);
                    studyFund -= studyFundWithdrawal;
                }

                // If still shortfall, negative cashflow remains
                incomeNet += portfolioWithdrawalGross - portfolioCapGainsTax + studyFundWithdrawal;
            }

            var netWorthEnd = portfolio + studyFund + pensionBalance;
            var netIncomeTotal = salaryNet + rsuNet + pensionPayoutNet + cfg.AdditionalIncome.AnnualNetIncome;
            var result = new YearResult(
                YearIndex: year,
                Age: age,
                SalaryGross: salaryGross,
                SalaryTax: salaryTax,
                SalaryNet: salaryNet,
                RsuVested: rsuVested,
                RsuTax: rsuTax,
                RsuNet: rsuNet,
                PensionPayoutGross: pensionPayoutGross,
                PensionTax: pensionTax,
                PensionPayoutNet: pensionPayoutNet,
                PortfolioStart: portfolioStart,
                PortfolioReturn: portfolioReturn,
                PortfolioWithdrawalGross: portfolioWithdrawalGross,
                PortfolioCapGainsTax: portfolioCapGainsTax,
                PortfolioEnd: portfolio,
                StudyFundStart: studyFundStart,
                StudyFundReturn: studyFundReturn,
                StudyFundWithdrawal: studyFundWithdrawal,
                StudyFundEnd: studyFund,
                NetIncome: netIncomeTotal,
                Expenses: expenses,
                NetCashflow: incomeNet - expenses,
                NetWorthEnd: netWorthEnd,
                PensionBalanceEnd: pensionBalance);

            list.Add(result);

            // Grow salary & expenses for next year
            salary *= 1 + cfg.Salary.AnnualSalaryGrowthRate;
        }

        return Task.FromResult<IReadOnlyList<YearResult>>(list);
    }

    private static decimal YearlyExpenses(AppConfig cfg, int yearIndex)
    {
        return cfg.Expenses.AnnualExpenses * Pow(1 + cfg.Expenses.AnnualExpensesGrowthRate, yearIndex);
    }

    private static decimal Pow(decimal x, int n)
    {
        var result = 1m;
        for (int i = 0; i < n; i++) result *= x;
        return result;
    }
}
