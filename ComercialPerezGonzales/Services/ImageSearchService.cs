using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace ComercialPerezGonzales.Services;

// ponytail: DuckDuckGo HTML scrape — gratis, sin API key, frágil si DDG cambia formato.
// Upgrade path: Bing/Google Custom Search con API key si esto deja de funcionar.
public class ImageSearchService
{
    private static readonly HttpClient _http = CrearHttp();

    private static HttpClient CrearHttp()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        h.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122 Safari/537.36");
        return h;
    }

    public async Task<byte[]?> DescargarPrimera(string consulta, CancellationToken ct = default)
    {
        try
        {
            var encQ = Uri.EscapeDataString(consulta);
            var landing = await _http.GetStringAsync($"https://duckduckgo.com/?q={encQ}&iax=images&ia=images", ct);

            var vqd = Regex.Match(landing, @"vqd=['""]([^'""&]+)['""]").Groups[1].Value;
            if (string.IsNullOrWhiteSpace(vqd))
                vqd = Regex.Match(landing, @"vqd=([\d-]+)").Groups[1].Value;
            if (string.IsNullOrWhiteSpace(vqd)) return null;

            var apiUrl = $"https://duckduckgo.com/i.js?l=us-en&o=json&q={encQ}&vqd={Uri.EscapeDataString(vqd)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            req.Headers.Referrer = new Uri($"https://duckduckgo.com/?q={encQ}&iax=images&ia=images");
            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results)) return null;

            foreach (var r in results.EnumerateArray())
            {
                if (!r.TryGetProperty("image", out var imgEl)) continue;
                var url = imgEl.GetString();
                if (string.IsNullOrWhiteSpace(url)) continue;
                try
                {
                    var bytes = await _http.GetByteArrayAsync(url, ct);
                    if (bytes.Length > 1024 && bytes.Length < 10_000_000) return bytes;
                }
                catch { /* siguiente candidato */ }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
