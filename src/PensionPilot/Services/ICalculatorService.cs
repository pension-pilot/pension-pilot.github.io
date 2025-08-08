using PensionPilot.Models.Config;

namespace PensionPilot.Services;

public interface ICalculatorService
{
    // Returns a per-year projection
    Task<IReadOnlyList<YearResult>> ProjectAsync(AppConfig config);
}

public record YearResult
(
    int YearIndex,
    int Age,

    decimal SalaryGross,
    decimal SalaryTax,
    decimal SalaryNet,

    decimal RsuVested,
    decimal RsuTax,
    decimal RsuNet,

    decimal PensionPayoutGross,
    decimal PensionTax,
    decimal PensionPayoutNet,

    decimal PortfolioStart,
    decimal PortfolioReturn,
    decimal PortfolioWithdrawalGross,
    decimal PortfolioCapGainsTax,
    decimal PortfolioEnd,

    decimal StudyFundStart,
    decimal StudyFundReturn,
    decimal StudyFundWithdrawal,
    decimal StudyFundEnd,

    decimal NetIncome,
    decimal Expenses,
    decimal NetCashflow,
    decimal NetWorthEnd,

    decimal PensionBalanceEnd
);
