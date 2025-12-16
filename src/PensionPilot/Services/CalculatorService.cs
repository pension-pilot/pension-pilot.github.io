using PensionPilot.Models.Config;

namespace PensionPilot.Services;

public class CalculatorService(ITaxService tax) : ICalculatorService
{
    public IReadOnlyList<YearResult> Project(AppConfig cfg)
    {
        var list = new List<YearResult>();

        var age = cfg.Timeline.CurrentAge;

        decimal portfolio = cfg.Portfolio.CurrentValue;
        decimal portfolioCostBasis = cfg.Portfolio.CurrentValue; // Track cost basis for capital gains
        decimal studyFund = cfg.StudyFund.CurrentValue;
        decimal pensionBalance = cfg.Pension.CurrentBalance;
        decimal salary = cfg.Salary.AnnualGrossSalary;
        decimal additionalIncome = cfg.AdditionalIncome.AnnualNetIncome;
        decimal? fixedPensionAnnuity = null; // Calculated once at pension age

        bool foundPortfolioReturnExceedsContribution = false;
        bool foundPortfolioReturnExceedsSalary = false;
        bool foundWithdrawalRateCoversExpenses = false;

        int pensionPayoutYear = 0; // Track years since pension started for annual adjustment

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

            // RSU: annual grant added to portfolio while working
            decimal rsuVested = 0;
            decimal rsuTax = 0;
            decimal rsuNet = 0;
            if (isWorking && cfg.Rsu.AnnualGrantAmount > 0)
            {
                rsuVested = cfg.Rsu.AnnualGrantAmount;
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
                // Calculate base annuity payment once at pension start
                if (fixedPensionAnnuity is null)
                {
                    var monthly = pensionBalance / Math.Max(1, cfg.Pension.AnnuityFactor);
                    fixedPensionAnnuity = monthly * 12;
                    // Balance no longer tracked - annuity pays for life
                    pensionBalance = 0;
                }

                // Annuity pays for life, adjusted annually by expense growth rate
                pensionPayoutGross = fixedPensionAnnuity.Value * Pow(1 + cfg.Expenses.AnnualExpensesGrowthRate, pensionPayoutYear);
                pensionPayoutYear++;

                if (pensionPayoutGross > 0)
                {
                    pensionTax = tax.CalculateIncomeTax(pensionPayoutGross, cfg.Taxes.IncomeTaxBrackets);
                    pensionPayoutNet = pensionPayoutGross - pensionTax;
                }
            }

            // Portfolio return first
            var portfolioStart = portfolio;
            var portfolioReturn = portfolio * cfg.Portfolio.AnnualReturnRate;
            portfolio += portfolioReturn;

            // Study fund return and add contributions
            var studyFundStart = studyFund;
            var studyFundReturn = studyFund * cfg.StudyFund.AnnualReturnRate;
            studyFund += studyFundReturn + employeeStudyFundContr + employerStudyFundContr;

            // Cashflows (additional income grows with inflation)
            decimal expenses = YearlyExpenses(cfg, year);
            decimal currentAdditionalIncome = additionalIncome * Pow(1 + cfg.Expenses.AnnualExpensesGrowthRate, year);
            decimal incomeNet = salaryNet + rsuNet + pensionPayoutNet + currentAdditionalIncome;

            // Add RSU net and any net salary savings + additional income to portfolio by default when working
            portfolio += rsuNet;
            portfolioCostBasis += rsuNet; // RSU net is after-tax, so full amount is cost basis
            if (isWorking)
            {
                // treat additional net income like salaryNet for savings
                var savings = Math.Max(0, salaryNet + currentAdditionalIncome - expenses);
                portfolio += savings;
                portfolioCostBasis += savings; // Savings are new contributions
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

                // Capital gains tax: tax on the gains portion of withdrawals based on cost basis
                if (cfg.Portfolio.TaxWithdrawalsAsCapitalGains && portfolioWithdrawalGross > 0 && portfolio + portfolioWithdrawalGross > portfolioCostBasis)
                {
                    // Calculate what fraction of current portfolio is gains vs cost basis
                    var totalBeforeWithdrawal = portfolio + portfolioWithdrawalGross;
                    var gainsFraction = Math.Max(0, (totalBeforeWithdrawal - portfolioCostBasis) / totalBeforeWithdrawal);
                    portfolioCapGainsTax = portfolioWithdrawalGross * gainsFraction * cfg.Taxes.CapitalGainsRate;
                    portfolio -= portfolioCapGainsTax;

                    // Reduce cost basis proportionally with withdrawal
                    var costBasisFraction = 1 - gainsFraction;
                    portfolioCostBasis -= portfolioWithdrawalGross * costBasisFraction;
                    portfolioCostBasis = Math.Max(0, portfolioCostBasis);
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

            // Achievement flags (only mark the first year they happen)
            bool markPortfolioReturnExceedsContribution = false;
            bool markPortfolioReturnExceedsSalary = false;
            bool markWithdrawalRateCoversExpenses = false;

            if (isWorking)
            {
                var savings = Math.Max(0, salaryNet + currentAdditionalIncome - expenses);
                var contributions = rsuNet + savings;
                if (!foundPortfolioReturnExceedsContribution && portfolioReturn > contributions)
                {
                    markPortfolioReturnExceedsContribution = true;
                    foundPortfolioReturnExceedsContribution = true;
                }

                if (!foundPortfolioReturnExceedsSalary && portfolioReturn > salaryGross)
                {
                    markPortfolioReturnExceedsSalary = true;
                    foundPortfolioReturnExceedsSalary = true;
                }
            }

            var swrIncome = portfolio * cfg.Portfolio.PrePensionWithdrawalRate;
            if (!foundWithdrawalRateCoversExpenses && swrIncome >= expenses)
            {
                markWithdrawalRateCoversExpenses = true;
                foundWithdrawalRateCoversExpenses = true;
            }
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
                PensionBalanceEnd: pensionBalance,
                PortfolioReturnExceedsContribution: markPortfolioReturnExceedsContribution,
                PortfolioReturnExceedsSalary: markPortfolioReturnExceedsSalary,
                WithdrawalRateCoversExpenses: markWithdrawalRateCoversExpenses);

            list.Add(result);

            // Grow salary & expenses for next year
            salary *= 1 + cfg.Salary.AnnualSalaryGrowthRate;
        }

        return list;
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
