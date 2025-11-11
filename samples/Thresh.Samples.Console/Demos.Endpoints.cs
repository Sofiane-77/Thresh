using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Thresh.Endpoints;

namespace Thresh.Samples.Console;

internal static partial class Demos
{
    public static async Task EndpointsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Demo.Endpoints");
        var summoner = sp.GetRequiredService<ILolSummonerApi>();
        var every = TimeSpan.FromSeconds(2);

        log.LogInformation("Polling ILolSummonerApi.GetCurrentAsync every {S}s. Press Ctrl+C to stop.", every.TotalSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var me = await summoner.GetCurrentAsync(ct);
                log.LogInformation("Endpoints: GET current-summoner | ← OK | displayName={Name}", me?.DisplayName ?? "(unknown)");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Endpoints call failed (is the League Client running?)");
            }

            try { await Task.Delay(every, ct); } catch { break; }
        }
    }
}
