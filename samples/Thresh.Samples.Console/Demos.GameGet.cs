using System;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Thresh.Game;

namespace Thresh.Samples.Console;

internal static partial class Demos
{
    public static async Task GameGetAsync(IServiceProvider sp, CancellationToken ct)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Demo.Game");
        var game = sp.GetRequiredService<IGameHttpClient>();
        var every = TimeSpan.FromSeconds(2);

        log.LogInformation("Polling /liveclientdata/activeplayer every {S}s. Press Ctrl+C to stop.", every.TotalSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var me = await game.GetAsync<JsonElement>("/liveclientdata/activeplayer", ct);
                var name = me.TryGetProperty("summonerName", out var n) ? n.GetString() : "(unknown)";
                log.LogInformation("GAME HTTP → GET activeplayer | ← OK | summonerName={Name}", name);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Game Client GET failed (are you in a live game?)");
            }

            try { await Task.Delay(every, ct); } catch { break; }
        }
    }
}
