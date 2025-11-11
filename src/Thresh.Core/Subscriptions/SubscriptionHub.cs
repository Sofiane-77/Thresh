using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using Thresh.Abstractions;

namespace Thresh.Core.Subscriptions;

/// <summary>
/// Thread-safe hub to manage subscriptions and dispatch LeagueEventEnvelope instances.
/// Reused by EventStream and in tests/replayers.
/// </summary>
internal sealed class SubscriptionHub
{
    private readonly ILogger _log;
    private readonly object _gate = new();
    private readonly List<ISub> _subs = new();

    public SubscriptionHub(ILogger log) => _log = log;

    public IDisposable Subscribe(string uri, Action<LeagueEventEnvelope> onEvent)
        => Add(new UriSub(uri, onEvent));

    public IDisposable Subscribe(Regex pattern, Action<LeagueEventEnvelope> onEvent)
        => Add(new RegexSub(pattern, onEvent));

    public IDisposable Subscribe<T>(string uri, Action<T> onData)
        => Add(new TypedSub<T>(uri, onData, _log));

    public void Dispatch(in LeagueEventEnvelope env)
    {
        ISub[] copy;
        lock (_gate)
        {
            copy = _subs.ToArray();
        }

        foreach (var s in copy)
        {
            if (!s.Matches(env.Uri))
            {
                continue;
            }

            try { s.Invoke(env); }
            catch (Exception ex) { _log.LogWarning(ex, "Subscriber threw for {Uri}", env.Uri); }
        }
    }

    private IDisposable Add(ISub s)
    {
        lock (_gate)
        {
            _subs.Add(s);
        }

        return new Token(s, Remove);
    }

    private void Remove(ISub s)
    {
        lock (_gate)
        {
            _subs.Remove(s);
        }

        s.Dispose();
    }

    private interface ISub : IDisposable
    {
        bool Matches(string uri);
        void Invoke(LeagueEventEnvelope env);
    }

    private sealed class UriSub(string uri, Action<LeagueEventEnvelope> act) : ISub
    {
        private readonly string _u = uri;
        private readonly Action<LeagueEventEnvelope> _a = act;
        public bool Matches(string uri) => string.Equals(uri, _u, StringComparison.OrdinalIgnoreCase);
        public void Invoke(LeagueEventEnvelope env) => _a(env);
        public void Dispose() { }
    }

    private sealed class RegexSub(Regex r, Action<LeagueEventEnvelope> act) : ISub
    {
        private readonly Regex _r = r;
        private readonly Action<LeagueEventEnvelope> _a = act;
        public bool Matches(string uri) => _r.IsMatch(uri);
        public void Invoke(LeagueEventEnvelope env) => _a(env);
        public void Dispose() { }
    }

    private sealed class TypedSub<T>(string uri, Action<T> act, ILogger log) : ISub
    {
        private readonly string _u = uri;
        private readonly Action<T> _a = act;
        public bool Matches(string uri) => string.Equals(uri, _u, StringComparison.OrdinalIgnoreCase);
        public void Invoke(LeagueEventEnvelope env)
        {
            try
            {
                // EN: Centralized Json options to reduce allocations and keep behavior consistent.
                var val = env.Data.Deserialize<T>(SharedJson.Options);
                if (val is not null)
                {
                    _a(val);
                }
            }
            catch (Exception ex)
            {
                log.LogDebug(ex, "Typed deserialization failed for {Uri}", _u);
            }
        }
        public void Dispose() { }
    }

    private sealed class Token : IDisposable
    {
        private readonly ISub _s; private readonly Action<ISub> _rm; private int _d;
        public Token(ISub s, Action<ISub> remove) { _s = s; _rm = remove; }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _d, 1) == 0)
            {
                _rm(_s);
            }
        }
    }
}
