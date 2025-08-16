namespace PensionPilot.Models.Config;

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
