using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using PensionPilot.Models.Config;

namespace PensionPilot.Services;

public class ConfigService(HttpClient http, IJSRuntime js) : IConfigService
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string StorageKey = "pensionpilot.config";
    private const string PreviewStorageKey = "pensionpilot.preview";
    private AppConfig? _cached;

    public async Task<AppConfig> GetConfigAsync()
    {
        if (_cached is not null) return _cached;
        _cached = await LoadFromStorageAsync(localStorage: true, fallbackToDefaults: true);
        return _cached!;
    }

    public async Task<AppConfig> GetSavedConfigAsync()
    {
        // Always read fresh from localStorage, bypassing cache
        return (await LoadFromStorageAsync(localStorage: true, fallbackToDefaults: true))!;
    }

    public Task<AppConfig?> GetPreviewConfigAsync() =>
        LoadFromStorageAsync(localStorage: false, fallbackToDefaults: false);

    public async Task<bool> HasPreviewConfigAsync()
    {
        try
        {
            var stored = await js.InvokeAsync<string?>("sessionStorage.getItem", PreviewStorageKey);
            return !string.IsNullOrWhiteSpace(stored);
        }
        catch
        {
            return false;
        }
    }

    public Task SetConfigAsync(AppConfig config) => SaveToStorageAsync(config, localStorage: true);

    public Task SetPreviewConfigAsync(AppConfig config) => SaveToStorageAsync(config, localStorage: false);

    public async Task ResetAsync()
    {
        _cached = null;
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    public async Task ClearPreviewConfigAsync()
    {
        await js.InvokeVoidAsync("sessionStorage.removeItem", PreviewStorageKey);
    }

    private async Task<AppConfig?> LoadFromStorageAsync(bool localStorage, bool fallbackToDefaults)
    {
        var storageType = localStorage ? "localStorage" : "sessionStorage";
        var key = localStorage ? StorageKey : PreviewStorageKey;

        try
        {
            var stored = await js.InvokeAsync<string?>(storageType + ".getItem", key);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                var cfg = JsonSerializer.Deserialize<AppConfig>(stored, jsonSerializerOptions);
                if (cfg is not null)
                {
                    InitializeConfig(cfg);
                    return cfg;
                }
            }

            if (!fallbackToDefaults) return null;

            // Fallback to defaults.json
            var defaultCfg = await http.GetFromJsonAsync<AppConfig>("config/defaults.json", jsonSerializerOptions) ?? new AppConfig();
            InitializeConfig(defaultCfg);

            // Persist defaults so future loads use localStorage
            var json = JsonSerializer.Serialize(defaultCfg, jsonSerializerOptions);
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
            return defaultCfg;
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is NotSupportedException || ex is JsonException || ex is JSException)
        {
            if (!fallbackToDefaults) return null;

            Console.Error.WriteLine($"Failed to load config: {ex.Message}");
            var cfg = new AppConfig();
            InitializeConfig(cfg);
            return cfg;
        }
    }

    private async Task SaveToStorageAsync(AppConfig config, bool localStorage)
    {
        InitializeConfig(config);
        var storageType = localStorage ? "localStorage" : "sessionStorage";
        var key = localStorage ? StorageKey : PreviewStorageKey;

        if (localStorage) _cached = config;

        var json = JsonSerializer.Serialize(config, jsonSerializerOptions);
        await js.InvokeVoidAsync(storageType + ".setItem", key, json);
    }

    private static void InitializeConfig(AppConfig cfg)
    {
        cfg.Taxes ??= new TaxesSettings();
        cfg.Taxes.IncomeTaxBrackets ??= [];
        cfg.Taxes.SocialSecurityTaxBrackets ??= [];
        cfg.Columns ??= [];
        foreach (var kvp in ColumnVisibilityDefaults.Defaults)
        {
            if (!cfg.Columns.ContainsKey(kvp.Key))
            {
                cfg.Columns[kvp.Key] = kvp.Value;
            }
        }
    }
}
