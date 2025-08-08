using System.Text.Json;
using PensionPilot.Models.Config;
using System.Text;
using System.IO.Compression;

namespace PensionPilot.Services;

public static class ConfigShareHelper
{
    private static readonly JsonSerializerOptions opts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Version 1 = GZip compressed UTF8 JSON (header byte 0x01)
    private const byte VersionGzipV1 = 1;

    public static string Encode(AppConfig cfg)
    {
        var json = JsonSerializer.Serialize(cfg, opts);
        var utf8 = Encoding.UTF8.GetBytes(json);
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(utf8, 0, utf8.Length);
        }
        var compressed = ms.ToArray();
        var payload = new byte[compressed.Length + 1];
        payload[0] = VersionGzipV1;
        Buffer.BlockCopy(compressed, 0, payload, 1, compressed.Length);
        return ToUrlSafeBase64(payload);
    }

    public static bool TryDecode(string? base64, out AppConfig? cfg)
    {
        cfg = null;
        if (string.IsNullOrWhiteSpace(base64)) return false;
        try
        {
            var bytes = FromUrlSafeBase64(base64);
            if (bytes.Length < 2) return false;
            if (bytes[0] != VersionGzipV1) return false; // unknown version
            using var cms = new MemoryStream(bytes, 1, bytes.Length - 1);
            using var gzip = new GZipStream(cms, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            gzip.CopyTo(outMs);
            var json = Encoding.UTF8.GetString(outMs.ToArray());
            cfg = JsonSerializer.Deserialize<AppConfig>(json, opts);
            return cfg is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string ToUrlSafeBase64(byte[] bytes)
    {
        var s = Convert.ToBase64String(bytes).TrimEnd('=');
        return s.Replace('+', '-').Replace('/', '_');
    }

    private static byte[] FromUrlSafeBase64(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
