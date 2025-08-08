namespace PensionPilot.Models.Tax;

public class IncomeTaxBracket
{
    public decimal? Value { get; set; }
    public decimal Rate { get; set; } // 0..1
}
