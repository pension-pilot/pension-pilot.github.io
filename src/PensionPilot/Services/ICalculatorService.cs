using PensionPilot.Models.Config;

namespace PensionPilot.Services;

public interface ICalculatorService
{
    // Returns a per-year projection
    Task<IReadOnlyList<YearResult>> ProjectAsync(AppConfig config);
}
