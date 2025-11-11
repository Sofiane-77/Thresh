using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Thresh.Abstractions;

namespace Thresh.Core.Tests;

/// <summary>
/// Lightweight test double for IEventStream.
/// - Implements the full interface (including Reconnected + WaitUntilConnectedAsync + LastMessageUtc).
/// - Uses SubscriptionHub internally so dispatch/typed subscriptions behave like the real stream.
/// - Exposes Publish(...) to push fake envelopes in tests.
/// </summary>
internal sealed class FakeEventStream : IEventStream
{
    public event EventHandler<string>? RawMessage;
    public event EventHandler<LeagueEventEnvelope>? Message;
    public event EventHandler? Reconnected;

    private readonly Core.Subscriptions.SubscriptionHub _hub;
    private volatile bool _isConnected;
    private TaskCompletionSource<bool> _connectedTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsConnected => _isConnected;

    // When we "publish" a message, update the timestamp to mimic the real stream.
    public DateTimeOffset? LastMessageUtc { get; private set; }

    public FakeEventStream(Microsoft.Extensions.Logging.ILoggerFactory lf)
        => _hub = new Core.Subscriptions.SubscriptionHub(lf.CreateLogger("Thresh.Subscriptions.Fake"));

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _isConnected = true;
        // Signal waiting callers that we are connected.
        if (!_connectedTcs.Task.IsCompleted)
            _connectedTcs.TrySetResult(true);

        // Emit the reconnection event like the real implementation.
        Reconnected?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task<bool> WaitUntilConnectedAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        if (IsConnected) return Task.FromResult(true);
        // Wait for either the connection TCS or the timeout.
        return Task.WhenAny(_connectedTcs.Task, Task.Delay(timeout, ct))
                   .ContinueWith(_ => IsConnected, ct);
    }

    public IDisposable Subscribe(string uri, Action<LeagueEventEnvelope> onEvent)
        => _hub.Subscribe(uri, onEvent);

    public IDisposable Subscribe(Regex uriPattern, Action<LeagueEventEnvelope> onEvent)
        => _hub.Subscribe(uriPattern, onEvent);

    public IDisposable Subscribe<T>(string uri, Action<T> onData, bool withSnapshot = false)
        // NOTE: withSnapshot is ignored in tests — the fake stream has no HTTP layer.
        => _hub.Subscribe<T>(uri, onData);

    /// <summary>
    /// Push a fake envelope into the stream: raises RawMessage, Message, and dispatches to subscriptions.
    /// </summary>
    public void Publish(LeagueEventEnvelope env)
    {
        // Build a raw WAMP-like message to match EventStream behavior for tests that listen to RawMessage.
        var raw = JsonSerializer.Serialize(new object[]
        {
            8, "OnJsonApiEvent", new { uri = env.Uri, eventType = env.EventType, data = env.Data }
        });

        LastMessageUtc = DateTimeOffset.UtcNow;

        RawMessage?.Invoke(this, raw);
        Message?.Invoke(this, env);
        _hub.Dispatch(env);
    }

    public ValueTask DisposeAsync()
    {
        _isConnected = false;

        // Reset the TCS so future WaitUntilConnectedAsync calls won't complete spuriously.
        if (_connectedTcs.Task.IsCompleted)
            _connectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        return ValueTask.CompletedTask;
    }
}
