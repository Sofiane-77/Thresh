using System.Diagnostics.Metrics;

namespace Thresh.Core.Observability;

public sealed class ThreshMetrics : IDisposable
{
    public const string MeterName = "Thresh";
    private readonly Meter _meter = new(MeterName);

    // HTTP
    public Counter<long> HttpRequests { get; }
    public Counter<long> HttpFailures { get; }
    public Histogram<double> HttpRequestMs { get; }

    // WS
    public UpDownCounter<long> WsConnections { get; }
    public Counter<long> WsReconnects { get; }
    public Counter<long> WsMessages { get; }
    public Counter<long> WsParseFailures { get; }

    public ThreshMetrics()
    {
        HttpRequests = _meter.CreateCounter<long>("http.requests.total");
        HttpFailures = _meter.CreateCounter<long>("http.failures.total");
        HttpRequestMs = _meter.CreateHistogram<double>("http.request.duration.ms");

        WsConnections = _meter.CreateUpDownCounter<long>("ws.connections.active");
        WsReconnects = _meter.CreateCounter<long>("ws.reconnects.total");
        WsMessages = _meter.CreateCounter<long>("ws.messages.total");
        WsParseFailures = _meter.CreateCounter<long>("ws.parse.failures.total");
    }

    public void Dispose() => _meter.Dispose();
}
