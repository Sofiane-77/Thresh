using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Thresh.Abstractions;

namespace Thresh.Hosting;

// EN: Hosted service that wires the EventStream lifecycle into a Generic Host (ASP.NET/Worker).
public sealed class ThreshHostedService(IEventStream ws, ILogger<ThreshHostedService> log) : IHostedService
{
    private readonly IEventStream _ws = ws;
    private readonly ILogger _log = log;

    public async Task StartAsync(CancellationToken ct)
    {
        await _ws.ConnectAsync(ct);
        _log.LogInformation("Thresh WS started");
    }

    public async Task StopAsync(CancellationToken ct)
    {
        await _ws.DisposeAsync();
        _log.LogInformation("Thresh WS stopped");
    }
}
