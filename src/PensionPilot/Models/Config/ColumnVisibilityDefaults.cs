namespace PensionPilot.Models.Config;

using PensionPilot.Services;

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
