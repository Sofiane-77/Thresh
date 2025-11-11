using System;

using Thresh.Abstractions;

namespace Thresh.Reactive;

/// <summary>Lightweight IObservable adapters over IEventStream (no external deps).</summary>
public static class EventStreamReactiveExtensions
{
    private static readonly System.Text.Json.JsonSerializerOptions _json =
        new(System.Text.Json.JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public static IObservable<LeagueEventEnvelope> Observe(this IEventStream stream, string uri)
        => Create<LeagueEventEnvelope>(obs => stream.Subscribe(uri, env => obs.OnNext(env)));

    public static IObservable<LeagueEventEnvelope> Observe(this IEventStream stream, System.Text.RegularExpressions.Regex uriPattern)
        => Create<LeagueEventEnvelope>(obs => stream.Subscribe(uriPattern, env => obs.OnNext(env)));

    public static IObservable<T> Observe<T>(this IEventStream stream, string uri, bool withSnapshot = false)
        => Create<T>(obs => stream.Subscribe<T>(uri, data => obs.OnNext(data), withSnapshot));

    public static IObservable<T> ObserveRegex<T>(this IEventStream stream, System.Text.RegularExpressions.Regex uriPattern)
        => Create<T>(obs => stream.Subscribe(uriPattern, env =>
        {
            try
            {
                var val = System.Text.Json.JsonSerializer.Deserialize<T>(env.Data, _json);
                if (val is not null)
                {
                    obs.OnNext(val);
                }
            }
            catch { /* ignore bad payload */ }
        }));

    public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate)
        => Create<T>(obs => source.Subscribe(v => { if (predicate(v)) { obs.OnNext(v); } }));

    public static IObservable<LeagueEventEnvelope> ObserveMany(this IEventStream stream, params string[] uris)
        => Create<LeagueEventEnvelope>(obs =>
        {
            var subs = uris.Select(u => stream.Subscribe(u, env => obs.OnNext(env))).ToArray();
            return new CompositeDisposable(subs);
        });

    // --- minimal observable helper ---
    private static IObservable<T> Create<T>(Func<IObserver<T>, IDisposable> subscribe)
        => new SimpleObservable<T>(subscribe);

    private sealed class SimpleObservable<T>(Func<IObserver<T>, IDisposable> subscribe) : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer) => subscribe(observer);
    }

    public static IObservable<LeagueEventEnvelope> ToObservable(this IEventStream stream)
        => Create<LeagueEventEnvelope>(obs =>
        {
            void Handler(object? _, LeagueEventEnvelope e) => obs.OnNext(e);
            stream.Message += Handler;
            return new Unsub(() => stream.Message -= Handler);
        });

    // small helper for unsubscription (within the same class)
    private sealed class Unsub : IDisposable
    {
        private readonly Action _dispose;
        private int _done;
        public Unsub(Action dispose) => _dispose = dispose;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _done, 1) == 0)
            {
                _dispose();
            }
        }
    }

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IDisposable[] _items; private int _d;
        public CompositeDisposable(IDisposable[] items) => _items = items;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _d, 1) != 0)
            {
                return;
            }

            foreach (var d in _items) { try { d.Dispose(); } catch { } }
        }
    }
}
