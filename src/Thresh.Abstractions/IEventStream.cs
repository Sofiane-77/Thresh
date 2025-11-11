using System.Text.RegularExpressions;

namespace Thresh.Abstractions;

/// <summary>LCU event stream (WAMP over WebSocket) with typed subscriptions.</summary>
public interface IEventStream : IAsyncDisposable
{
    /// <summary>Raw WAMP text message event.</summary>
    event EventHandler<string>? RawMessage;

    /// <summary>Parsed "envelope" event after WAMP decoding.</summary>
    event EventHandler<LeagueEventEnvelope>? Message;

    /// <summary>Raised after a successful connection or reconnection.</summary>
    event EventHandler? Reconnected;

    /// <summary>UTC timestamp of the last received message (if any).</summary>
    DateTimeOffset? LastMessageUtc { get; }

    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>True when the socket is currently connected.</summary>
    bool IsConnected { get; }

    /// <summary>Wait (at most "timeout") for the WebSocket to become connected.</summary>
    Task<bool> WaitUntilConnectedAsync(TimeSpan timeout, CancellationToken ct = default);

    IDisposable Subscribe(string uri, Action<LeagueEventEnvelope> onEvent);
    IDisposable Subscribe(Regex uriPattern, Action<LeagueEventEnvelope> onEvent);

    /// <summary>Typed subscription; when withSnapshot=true, performs an initial GET on the URI and emits the value.</summary>
    IDisposable Subscribe<T>(string uri, Action<T> onData, bool withSnapshot = false);
}
