using System;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Thresh.Abstractions;

namespace Thresh.Samples.Console;

internal static partial class Demos
{
    public static async Task HttpGetAsync(IServiceProvider sp, CancellationToken ct)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Demo.Http");
        var api = sp.GetRequiredService<ILcuHttpClient>();
        var every = TimeSpan.FromSeconds(2);

        log.LogInformation("Polling /lol-summoner/v1/current-summoner every {S}s. Press Ctrl+C to stop.", every.TotalSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var me = await api.GetAsync<JsonElement>("/lol-summoner/v1/current-summoner", ct);
                var name = me.TryGetProperty("displayName", out var n) ? n.GetString() : "(unknown)";
                log.LogInformation("HTTP → GET current-summoner | ← OK | displayName={Name}", name);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                log.LogWarning(ex, "HTTP GET failed (is the League Client running?)");
            }

            try { await Task.Delay(every, ct); } catch { break; }
        }
    }
}
