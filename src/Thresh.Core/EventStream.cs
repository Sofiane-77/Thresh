using System.Buffers;
using System.Diagnostics;
using System.Net.Security;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Thresh.Abstractions;
using Thresh.Core.Observability;
using Thresh.Core.Subscriptions;

namespace Thresh.Core;

public sealed class EventStream : IEventStream
{
    private const int MaxMessageChars = 2 * 1024 * 1024; // Safety rail: drop oversized WS frames (2 MB)

    private readonly ILockfileWatcher _watcher;
    private readonly ILcuHttpClient _api;
    private readonly ThreshOptions _opts;
    private readonly ILogger<EventStream> _log;
    private readonly ThreshMetrics _m;

    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;
    private volatile bool _connected;

    private readonly SubscriptionHub _hub;
    private readonly Random _rng = new();

    // Start gate so ConnectAsync is idempotent and thread-safe
    private int _started = 0;

    // Connection signaling (no polling)
    private TaskCompletionSource<bool> _connectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public event EventHandler<string>? RawMessage;
    public event EventHandler<LeagueEventEnvelope>? Message;
    public event EventHandler? Reconnected;

    public bool IsConnected => _connected;
    public DateTimeOffset? LastMessageUtc { get; private set; }

    public EventStream(ILockfileWatcher watcher, ILcuHttpClient api, IOptions<ThreshOptions> opts,
                       ILogger<EventStream> log, ThreshMetrics metrics, ILoggerFactory lf)
    {
        _watcher = watcher;
        _api = api;
        _opts = opts.Value;
        _log = log;
        _m = metrics;
        _hub = new SubscriptionHub(lf.CreateLogger("Thresh.Subscriptions"));
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        // Ensure the run loop is started only once (even under concurrent calls)
        if (Interlocked.Exchange(ref _started, 1) == 0)
        {
            _loop = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
        }
        return Task.CompletedTask;
    }

    public async Task<bool> WaitUntilConnectedAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        // Simpler and less error-prone than ContinueWith-based approach
        if (IsConnected) return true;
        var tcs = _connectedTcs.Task;
        var winner = await Task.WhenAny(tcs, Task.Delay(timeout, ct));
        return ReferenceEquals(winner, tcs) && IsConnected;
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_loop is not null) { try { await _loop; } catch { } }
        _cts.Dispose();
    }

    private void SignalConnected()
    {
        if (!_connectedTcs.Task.IsCompleted)
            _connectedTcs.TrySetResult(true);
    }

    private void ResetConnectedSignalIfCompleted()
    {
        if (_connectedTcs.Task.IsCompleted)
            _connectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        var backoff = _opts.BaseRetryDelay;
        var maxBackoff = _opts.WsMaxBackoff;
        var silenceThreshold = _opts.WsSilenceThreshold;
        DateTime lastMsgUtc = DateTime.UtcNow;
        LastMessageUtc = lastMsgUtc;

        while (!ct.IsCancellationRequested)
        {
            ClientWebSocket? ws = null;
            byte[]? buffer = null;
            try
            {
                var creds = await _watcher.GetCurrentAsync(ct);

                using var connectAct = ThreshTracing.Source.StartActivity("ws.connect", ActivityKind.Client);
                connectAct?.SetTag("net.peer.name", "127.0.0.1");
                connectAct?.SetTag("net.transport", "ip-tcp");
                connectAct?.SetTag("server.port", creds.Port);

                ws = new ClientWebSocket();
                ws.Options.AddSubProtocol("wamp");
                ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

                if (_opts.WsAcceptSelfSignedCertificates)
                {
                    // Narrow acceptance to expected LCU issues on loopback (safer than 'return true').
                    ws.Options.RemoteCertificateValidationCallback = static (sender, cert, chain, errors) =>
                    {
                        const SslPolicyErrors allowed =
                            SslPolicyErrors.RemoteCertificateChainErrors |
                            SslPolicyErrors.RemoteCertificateNameMismatch;
                        return errors == SslPolicyErrors.None || (errors & ~allowed) == 0;
                    };
                }

                ws.Options.SetRequestHeader("Authorization", $"Basic {creds.BasicAuthValue}");

                var uri = new Uri($"wss://127.0.0.1:{creds.Port}/");
                _log.LogInformation(LogEvents.WsConnecting, "WS connecting to {Uri} ...", uri);

                await ws.ConnectAsync(uri, ct);

                // Mark connection established
                _connected = true;
                backoff = _opts.BaseRetryDelay; // Reset backoff after successful connection
                SignalConnected();

                _m.WsConnections.Add(1);
                _m.WsReconnects.Add(1);
                _log.LogInformation(LogEvents.WsConnected, "WS connected.");
                Reconnected?.Invoke(this, EventArgs.Empty);

                var subscribe = Encoding.UTF8.GetBytes("[5, \"OnJsonApiEvent\"]");
                await ws.SendAsync(subscribe, WebSocketMessageType.Text, endOfMessage: true, ct);

                // Rent a large buffer to reduce per-message allocations
                buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
                var sb = new StringBuilder(capacity: 64 * 1024);

                // Watchdog: if we get no messages for too long, force-close to trigger a reconnect.
                using var watchdogCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var watchdog = Task.Run(async () =>
                {
                    while (!watchdogCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), watchdogCts.Token);
                        if (DateTime.UtcNow - lastMsgUtc > silenceThreshold)
                        {
                            try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "silence watchdog", CancellationToken.None); } catch { }
                            break;
                        }
                    }
                }, watchdogCts.Token);

                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    // Drop oversized frames to avoid runaway memory usage / malicious payloads.
                    if (sb.Length > MaxMessageChars)
                    {
                        _m.WsParseFailures.Add(1);
                        _log.LogWarning(LogEvents.WsParseFailed, "WS message too large (> {Max} chars); dropping", MaxMessageChars);
                        sb.Clear();
                        continue;
                    }

                    if (result.EndOfMessage)
                    {
                        var msg = sb.ToString();
                        sb.Clear();
                        lastMsgUtc = DateTime.UtcNow;
                        LastMessageUtc = lastMsgUtc;

                        // Protect RawMessage handlers from throwing and breaking the loop
                        try { RawMessage?.Invoke(this, msg); }
                        catch (Exception ex)
                        {
                            _log.LogWarning(LogEvents.WsDispatchErr, ex, "RawMessage handler threw");
                        }

                        using var msgAct = ThreshTracing.Source.StartActivity("ws.message", ActivityKind.Client);
                        msgAct?.SetTag("messaging.operation", "process");
                        msgAct?.SetTag("message.size", msg.Length);

                        try
                        {
                            using var doc = JsonDocument.Parse(msg);
                            // WAMP: [8, "OnJsonApiEvent", { .. }]
                            if (doc.RootElement is { ValueKind: JsonValueKind.Array } root && root.GetArrayLength() >= 3)
                            {
                                var body = root[2];
                                if (body.ValueKind == JsonValueKind.Object &&
                                    body.TryGetProperty("uri", out var u) &&
                                    body.TryGetProperty("eventType", out var et) &&
                                    body.TryGetProperty("data", out var data))
                                {
                                    var env = new LeagueEventEnvelope
                                    {
                                        Uri = u.GetString() ?? "",
                                        EventType = et.GetString() ?? "",
                                        Data = data.Clone()
                                    };

                                    _m.WsMessages.Add(1);

                                    // Protect Message handlers from throwing and misreporting as parse errors
                                    try { Message?.Invoke(this, env); }
                                    catch (Exception ex)
                                    {
                                        _log.LogWarning(LogEvents.WsDispatchErr, ex, "Message handler threw for {Uri}", env.Uri);
                                    }

                                    _hub.Dispatch(env);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _m.WsParseFailures.Add(1);
                            _log.LogWarning(LogEvents.WsParseFailed, ex, "WS parse failed (first 120 chars: {Preview})",
                                msg[..Math.Min(120, msg.Length)]);
                        }
                    }
                }
                try { watchdogCts.Cancel(); await watchdog; } catch { }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "WS error; will retry.");
            }
            finally
            {
                if (_connected) { _m.WsConnections.Add(-1); }
                _connected = false;
                ResetConnectedSignalIfCompleted();

                try
                {
                    if (ws is { State: WebSocketState.Open })
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                    }
                }
                catch { }
                ws?.Dispose();

                if (buffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                _log.LogInformation(LogEvents.WsDisconnected, "WS disconnected.");
            }

            if (!ct.IsCancellationRequested)
            {
                // Exponential backoff with jitter for reconnect attempts
                var jitter = TimeSpan.FromMilliseconds(_rng.Next(0, 200));
                await Task.Delay(backoff + jitter, ct);
                backoff = TimeSpan.FromMilliseconds(Math.Min(backoff.TotalMilliseconds * 2, maxBackoff.TotalMilliseconds));
            }
        }
    }

    // --- IEventStream API → delegate to the hub + optional initial snapshot via _api
    public IDisposable Subscribe(string uri, Action<LeagueEventEnvelope> onEvent)
        => _hub.Subscribe(uri, onEvent);

    public IDisposable Subscribe(Regex uriPattern, Action<LeagueEventEnvelope> onEvent)
        => _hub.Subscribe(uriPattern, onEvent);

    public IDisposable Subscribe<T>(string uri, Action<T> onData, bool withSnapshot = false)
    {
        var token = _hub.Subscribe<T>(uri, onData);
        if (withSnapshot)
        {
            _ = EmitSnapshotAsync(uri, onData);
        }
        return token;
    }

    private async Task EmitSnapshotAsync<T>(string uri, Action<T> onData)
    {
        try
        {
            var snapshot = await _api.GetAsync<T>(uri);
            if (snapshot is not null)
            {
                onData(snapshot);
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Snapshot fetch failed for {Uri}", uri);
        }
    }
}
