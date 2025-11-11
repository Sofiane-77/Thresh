using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Thresh.Abstractions;
using Thresh.Reactive;

namespace Thresh.Samples.Console;

internal static partial class Demos
{
    public static async Task ReactiveAsync(IServiceProvider sp, CancellationToken ct)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Demo.Reactive");
        var ws = sp.GetRequiredService<IEventStream>();
        await ws.ConnectAsync(ct);

        log.LogInformation("Observing /lol-champ-select/v1/session (no snapshot). Press Ctrl+C to stop.");
        using var sub = ws.Observe<System.Text.Json.JsonElement>("/lol-champ-select/v1/session", withSnapshot: false)
            .Subscribe(_ => log.LogInformation("Reactive: Champ Select update received"));

        try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); } catch { }
    }
}
