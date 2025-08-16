namespace PensionPilot.Models.Config;

public class StudyFundSettings
{
    public decimal CurrentValue { get; set; }
    public decimal AnnualReturnRate { get; set; } = 0.04m;
    public int? ActiveFromAge { get; set; } // If null, study fund contributions are inactive
    public decimal? MaxAnnualContribution { get; set; } // If null, no cap; otherwise cap total (employee+employer)
}
