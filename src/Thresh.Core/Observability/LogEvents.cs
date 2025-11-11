using Microsoft.Extensions.Logging;

namespace Thresh.Core.Observability;

public static class LogEvents
{
    public static readonly EventId WsConnecting = new(1000, nameof(WsConnecting));
    public static readonly EventId WsConnected = new(1001, nameof(WsConnected));
    public static readonly EventId WsDisconnected = new(1002, nameof(WsDisconnected));
    public static readonly EventId WsParseFailed = new(1010, nameof(WsParseFailed));
    public static readonly EventId WsDispatchErr = new(1011, nameof(WsDispatchErr));

    public static readonly EventId HttpStart = new(2000, nameof(HttpStart));
    public static readonly EventId HttpStop = new(2001, nameof(HttpStop));
    public static readonly EventId HttpRetry = new(2010, nameof(HttpRetry));
    public static readonly EventId CircuitEvt = new(2020, nameof(CircuitEvt));
}
