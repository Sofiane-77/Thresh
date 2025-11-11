using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Thresh.Abstractions;

namespace Thresh.Samples.Console;

internal static partial class Demos
{
    public static async Task WsConnectAsync(IServiceProvider sp, CancellationToken ct)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Demo.WS");
        var ws = sp.GetRequiredService<IEventStream>();

        await ws.ConnectAsync(ct);
        log.LogInformation("WS connected = {State}. Press Ctrl+C to stop.", ws.IsConnected);

        int count = 0;
        void Handler(object? _, string msg)
        {
            count++;
            var preview = msg[..Math.Min(120, msg.Length)];
            System.Console.WriteLine($"WS raw[{count}]: {preview}...");
        }
        ws.RawMessage += Handler;

        try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); }
        catch { }
        finally { ws.RawMessage -= Handler; }
    }
}
