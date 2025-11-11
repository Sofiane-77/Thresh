using System.Net.Http.Headers;

using Microsoft.Extensions.Logging;

using Thresh.Abstractions;
using Thresh.Core.Observability;

namespace Thresh.Core.Handlers;

public sealed class LcuAuthHandler : DelegatingHandler
{
    private readonly ILockfileWatcher _watcher;
    private readonly ILogger<LcuAuthHandler> _log;

    public LcuAuthHandler(ILockfileWatcher watcher, ILogger<LcuAuthHandler> log)
    { _watcher = watcher; _log = log; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var creds = await _watcher.GetCurrentAsync(ct);
        if (!request.RequestUri!.IsAbsoluteUri)
        {
            request.RequestUri = new Uri(creds.BaseAddress, request.RequestUri);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds.BasicAuthValue);

        using var _ = _log.BeginScope(new Dictionary<string, object?>
        { ["method"] = request.Method.Method, ["path"] = request.RequestUri!.AbsolutePath });

        _log.LogDebug(LogEvents.HttpStart, "HTTP → {Method} {Path}", request.Method, request.RequestUri!.PathAndQuery);
        var res = await base.SendAsync(request, ct);
        _log.LogDebug(LogEvents.HttpStop, "HTTP ← {Status}", (int)res.StatusCode);
        return res;
    }
}
