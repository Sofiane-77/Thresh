using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Thresh.Abstractions;
using Thresh.Core.Observability;

namespace Thresh.Core;

public sealed class LcuHttpClient : ILcuHttpClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;
    private readonly ILogger<LcuHttpClient> _log;
    private readonly ThreshMetrics _m;

    public LcuHttpClient(HttpClient httpClient, ILogger<LcuHttpClient> log, ThreshMetrics metrics)
    {
        _http = httpClient;
        _log = log;
        _m = metrics;
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    }

    public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);

        using var act = ThreshTracing.Source.StartActivity("lcu.http", ActivityKind.Client);
        act?.SetTag("http.method", "GET");
        act?.SetTag("http.route", path);
        act?.SetTag("net.peer.name", "127.0.0.1");

        var sw = Stopwatch.StartNew();
        _log.LogDebug(Observability.LogEvents.HttpStart, "HTTP → GET {Path}", path);
        _m.HttpRequests.Add(1);

        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        sw.Stop();
        _m.HttpRequestMs.Record(sw.Elapsed.TotalMilliseconds);
        _log.LogDebug(Observability.LogEvents.HttpStop, "HTTP ← {Status} {Path} ({ElapsedMs:N1} ms)",
            (int)res.StatusCode, req.RequestUri!.PathAndQuery, sw.Elapsed.TotalMilliseconds);

        act?.SetTag("http.status_code", (int)res.StatusCode);

        if (!res.IsSuccessStatusCode)
        {
            _m.HttpFailures.Add(1);
            var payload = await res.Content.ReadAsStringAsync(ct);
            _log.LogWarning("HTTP {Status} on {Path}: {Preview}",
                (int)res.StatusCode, req.RequestUri!.PathAndQuery,
                payload[..Math.Min(120, payload.Length)]);
        }
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
