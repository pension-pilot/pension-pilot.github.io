namespace PensionPilot.Models.Config;

public class SalarySettings
{
    public decimal AnnualGrossSalary { get; set; }
    public decimal AnnualSalaryGrowthRate { get; set; } = 0.02m;

    public decimal EmployeePensionContributionRate { get; set; }
    public decimal EmployerPensionContributionRate { get; set; }

    public decimal EmployeeStudyFundContributionRate { get; set; }
    public decimal EmployerStudyFundContributionRate { get; set; }
}
