using PensionPilot.Models.Config;

namespace PensionPilot.Services;

public interface IConfigService
{
    Task<AppConfig> GetConfigAsync();
    Task SetConfigAsync(AppConfig config);
    Task ResetAsync();
}
