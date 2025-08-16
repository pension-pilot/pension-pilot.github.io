namespace PensionPilot.Models.Config;

using PensionPilot.Models.Tax;

public class TaxesSettings
{
    public List<IncomeTaxBracket> IncomeTaxBrackets { get; set; } = [];
    public List<IncomeTaxBracket> SocialSecurityTaxBrackets { get; set; } = [];
    public decimal CapitalGainsRate { get; set; } = 0.25m;
}
