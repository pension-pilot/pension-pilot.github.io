namespace PensionPilot.Models.Config;

public class ExpensesSettings
{
    public decimal AnnualExpenses { get; set; }
    public decimal AnnualExpensesGrowthRate { get; set; } = 0.02m;
}
