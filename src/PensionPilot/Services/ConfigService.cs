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
    private AppConfig? _cached;

    public async Task<AppConfig> GetConfigAsync()
    {
        if (_cached is not null) return _cached;
        try
        {
            // Try to load from localStorage first
            var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                var fromStorage = JsonSerializer.Deserialize<AppConfig>(stored!, jsonSerializerOptions) ?? new AppConfig();
                fromStorage.Taxes.IncomeTaxBrackets ??= [];
                fromStorage.Taxes.SocialSecurityTaxBrackets ??= [];
                EnsureColumnDefaults(fromStorage);
                _cached = fromStorage;
                return _cached;
            }

            // Fallback to defaults.json
            var cfg = await http.GetFromJsonAsync<AppConfig>("config/defaults.json", jsonSerializerOptions) ?? new AppConfig();
            cfg.Taxes.IncomeTaxBrackets ??= [];
            cfg.Taxes.SocialSecurityTaxBrackets ??= [];
            EnsureColumnDefaults(cfg);
            _cached = cfg;

            // Persist defaults so future loads use localStorage
            var json = JsonSerializer.Serialize(_cached, jsonSerializerOptions);
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is NotSupportedException || ex is JsonException || ex is JSException)
        {
            Console.Error.WriteLine($"Failed to load config: {ex.Message}");
            var cfg = new AppConfig
            {
                Taxes = new TaxesSettings
                {
                    IncomeTaxBrackets = [],
                    SocialSecurityTaxBrackets = []
                }
            };
            EnsureColumnDefaults(cfg);
            _cached = cfg;
        }
        return _cached!;
    }

    public async Task SetConfigAsync(AppConfig config)
    {
        config.Taxes.IncomeTaxBrackets ??= [];
        config.Taxes.SocialSecurityTaxBrackets ??= [];
        EnsureColumnDefaults(config);
        _cached = config;
        var json = JsonSerializer.Serialize(config, jsonSerializerOptions);
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task ResetAsync()
    {
        _cached = null;
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    private static void EnsureColumnDefaults(AppConfig cfg)
    {
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
