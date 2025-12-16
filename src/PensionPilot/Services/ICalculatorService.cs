using PensionPilot.Models.Config;

namespace PensionPilot.Services;

public interface ICalculatorService
{
    // Returns a per-year projection
    IReadOnlyList<YearResult> Project(AppConfig config);
}
