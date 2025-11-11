using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Thresh.Reactive;
using Thresh.Abstractions;
using Thresh.Domain;
using Thresh.Domain.Gameflow;

namespace Thresh.Samples.Console;

internal static partial class Demos
{
    public static async Task DomainAsync(IServiceProvider sp, CancellationToken ct)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Demo.Domain");
        var ws = sp.GetRequiredService<IEventStream>();
        await ws.ConnectAsync(ct);

        var gf = sp.GetRequiredService<IGameflowService>();
        gf.Start(ws);

        using var sub = gf.PhaseChanged.Subscribe(e => log.LogInformation("[Domain] Phase = {Phase}", e.Phase));
        log.LogInformation("Domain service running. Press Ctrl+C to stop.");

        try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); } catch { }
    }
}
