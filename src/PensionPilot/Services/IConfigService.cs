using PensionPilot.Models.Config;

namespace PensionPilot.Services;

public interface IConfigService
{
    Task<AppConfig> GetConfigAsync();
    /// <summary>
    /// Gets the config directly from localStorage, bypassing any in-memory cache.
    /// Useful for comparison when the cached config may have been mutated.
    /// </summary>
    Task<AppConfig> GetSavedConfigAsync();
    Task SetConfigAsync(AppConfig config);
    Task ResetAsync();
    
    /// <summary>
    /// Gets the preview config from sessionStorage, if any.
    /// </summary>
    Task<AppConfig?> GetPreviewConfigAsync();
    /// <summary>
    /// Saves a preview config to sessionStorage for comparison.
    /// </summary>
    Task SetPreviewConfigAsync(AppConfig config);
    /// <summary>
    /// Clears the preview config from sessionStorage.
    /// </summary>
    Task ClearPreviewConfigAsync();
    /// <summary>
    /// Returns true if there is a preview config stored.
    /// </summary>
    Task<bool> HasPreviewConfigAsync();
}
