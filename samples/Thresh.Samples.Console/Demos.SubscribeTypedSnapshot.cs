using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Thresh.Abstractions;

namespace Thresh.Samples.Console;

internal static partial class Demos
{
    public static async Task SubscribeTypedSnapshotAsync(IServiceProvider sp, CancellationToken ct)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Demo.TypedSnapshot");
        var ws = sp.GetRequiredService<IEventStream>();
        await ws.ConnectAsync(ct);

        using var sub = ws.Subscribe<System.Text.Json.JsonElement>("/lol-gameflow/v1/session", data =>
        {
            var phase = data.TryGetProperty("phase", out var p) ? p.GetString() : "?";
            log.LogInformation("Gameflow phase = {Phase}", phase);
        }, withSnapshot: true);

        log.LogInformation("Subscribed /lol-gameflow/v1/session with snapshot. Press Ctrl+C to stop.");
        try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); } catch { }
    }
}
