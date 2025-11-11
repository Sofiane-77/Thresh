using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;


namespace Thresh.Game;

public sealed class GameHttpClient : IGameHttpClient
{
    private readonly HttpClient _http;
    private readonly ILogger<GameHttpClient> _log;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };


    public GameHttpClient(HttpClient http, ILogger<GameHttpClient> log)
    { _http = http; _log = log; }


    public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        var sw = Stopwatch.StartNew();
        _log.LogDebug("GAME HTTP → GET {Path}", path);
        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        sw.Stop();
        _log.LogDebug("GAME HTTP ← {Status} {Path} ({ElapsedMs:N1} ms)", (int)res.StatusCode, req.RequestUri!.PathAndQuery, sw.Elapsed.TotalMilliseconds);
        res.EnsureSuccessStatusCode();
        if (res.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        await using var s = await res.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(s, _json, ct);
    }


    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
    => _http.SendAsync(request, ct);
}
