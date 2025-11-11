using System;
using System.Text.RegularExpressions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Thresh.Abstractions;

namespace Thresh.Samples.Console;

internal static partial class Demos
{
    public static async Task SubscribeRegexAsync(IServiceProvider sp, CancellationToken ct)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Demo.SubRegex");
        var ws = sp.GetRequiredService<IEventStream>();
        await ws.ConnectAsync(ct);

        // In a verbatim string (@"..."), \d must be written with a single backslash.
        // This regex now correctly matches versioned LCU endpoints like /lol-.../v1/...select
        using var sub = ws.Subscribe(new Regex(@"^/lol-.*/v\d+/.*select", RegexOptions.IgnoreCase), env =>
        {
            log.LogInformation("Regex match → [{Type}] {Uri} (len={Len})", env.EventType, env.Uri, env.Data.GetRawText().Length);
        });

        log.LogInformation("Subscribed with Regex. Press Ctrl+C to stop.");
        try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); } catch { }
    }
}
