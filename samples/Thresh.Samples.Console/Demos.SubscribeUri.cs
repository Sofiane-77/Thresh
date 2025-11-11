using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Thresh.Abstractions;

namespace Thresh.Samples.Console;

internal static partial class Demos
{
    public static async Task SubscribeUriAsync(IServiceProvider sp, CancellationToken ct)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Demo.SubUri");
        var ws = sp.GetRequiredService<IEventStream>();
        await ws.ConnectAsync(ct);

        using var sub = ws.Subscribe("/lol-lobby/v2/lobby", env =>
        {
            log.LogInformation("[{Type}] {Uri} payload len={Len}", env.EventType, env.Uri, env.Data.GetRawText().Length);
        });

        log.LogInformation("Subscribed to /lol-lobby/v2/lobby. Press Ctrl+C to stop.");
        try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); } catch { }
    }
}
