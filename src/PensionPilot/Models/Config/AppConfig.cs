namespace PensionPilot.Models.Config;

using PensionPilot.Models.Tax;
using PensionPilot.Services;

public class AppConfig
{
    public TimelineSettings Timeline { get; set; } = new();
    public TaxesSettings Taxes { get; set; } = new();
    public SalarySettings Salary { get; set; } = new();
    public RsuSettings Rsu { get; set; } = new();
    public PortfolioSettings Portfolio { get; set; } = new();
    public StudyFundSettings StudyFund { get; set; } = new();
    public PensionSettings Pension { get; set; } = new();
    public ExpensesSettings Expenses { get; set; } = new();
    public AdditionalIncomeSettings AdditionalIncome { get; set; } = new();
    public Dictionary<string, bool> Columns { get; set; } = [];
}

public static class ColumnVisibilityDefaults
{
    public static readonly IReadOnlyDictionary<string, bool> Defaults = new Dictionary<string, bool>
    {
        [nameof(YearResult.SalaryNet)] = true,
        [nameof(YearResult.RsuNet)] = true,
        [nameof(YearResult.PensionPayoutNet)] = true,
        [nameof(YearResult.NetIncome)] = true,
        [nameof(YearResult.NetCashflow)] = true,
        [nameof(YearResult.Expenses)] = true,
        [nameof(YearResult.NetWorthEnd)] = true,
    };
}

public class TimelineSettings
{
    public int CurrentAge { get; set; } = 20;
    public int RetirementAge { get; set; } = 50;
    public int PensionAge { get; set; } = 67;
    public int ModelEndAge { get; set; } = 100;
}

public class TaxesSettings
{
    public List<IncomeTaxBracket> IncomeTaxBrackets { get; set; } = [];
    public List<IncomeTaxBracket> SocialSecurityTaxBrackets { get; set; } = [];
    public decimal CapitalGainsRate { get; set; } = 0.25m;
}

public class SalarySettings
{
    public decimal AnnualGrossSalary { get; set; }
    public decimal AnnualSalaryGrowthRate { get; set; } = 0.02m;

    public decimal EmployeePensionContributionRate { get; set; }
    public decimal EmployerPensionContributionRate { get; set; }

    public decimal EmployeeStudyFundContributionRate { get; set; }
    public decimal EmployerStudyFundContributionRate { get; set; }
}

public class RsuSettings
{
    public decimal AnnualGrantAmount { get; set; }
    public int VestingYears { get; set; } = 4;
}

public class PortfolioSettings
{
    public decimal CurrentValue { get; set; }
    public decimal AnnualReturnRate { get; set; } = 0.05m;
    public decimal PrePensionWithdrawalRate { get; set; } = 0.035m; // between retirement and pension age
    public bool TaxWithdrawalsAsCapitalGains { get; set; } = true;
}

public class StudyFundSettings
{
    public decimal CurrentValue { get; set; }
    public decimal AnnualReturnRate { get; set; } = 0.04m;
    public int? ActiveFromAge { get; set; } // If null, study fund contributions are inactive
    public decimal? MaxAnnualContribution { get; set; } // If null, no cap; otherwise cap total (employee+employer)
}

public class PensionSettings
{
    public decimal CurrentBalance { get; set; }
    public decimal AnnualReturnRateUntilPension { get; set; } = 0.04m;
    public int AnnuityFactor { get; set; } = 220;
}

public class ExpensesSettings
{
    public decimal AnnualExpenses { get; set; }
    public decimal AnnualExpensesGrowthRate { get; set; } = 0.02m;
}

public class AdditionalIncomeSettings
{
    public decimal AnnualNetIncome { get; set; } // treated as already-net recurring yearly income
}
