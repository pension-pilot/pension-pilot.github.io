namespace PensionPilot.Models.Config;

public class RsuSettings
{
    public decimal AnnualGrantAmount { get; set; }
    public int VestingYears { get; set; } = 4;
}
