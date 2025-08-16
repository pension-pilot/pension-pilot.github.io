namespace PensionPilot.Models.Config;

public class PortfolioSettings
{
    public decimal CurrentValue { get; set; }
    public decimal AnnualReturnRate { get; set; } = 0.05m;
    public decimal PrePensionWithdrawalRate { get; set; } = 0.035m; // between retirement and pension age
    public bool TaxWithdrawalsAsCapitalGains { get; set; } = true;
}
