namespace PensionPilot.Models.Config;

public class PensionSettings
{
    public decimal CurrentBalance { get; set; }
    public decimal AnnualReturnRateUntilPension { get; set; } = 0.04m;
    public int AnnuityFactor { get; set; } = 220;
}
