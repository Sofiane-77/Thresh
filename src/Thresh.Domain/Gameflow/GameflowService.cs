using System;
using System.Text.Json;

using Thresh.Abstractions;
using Thresh.Reactive;

namespace Thresh.Domain.Gameflow;

public interface IGameflowService
{
    IObservable<GameflowPhaseChanged> PhaseChanged { get; }
    void Start(IEventStream stream);
}

public sealed class GameflowService : IGameflowService
{
    private IObservable<GameflowPhaseChanged>? _phaseObs;

    public IObservable<GameflowPhaseChanged> PhaseChanged
        => _phaseObs ?? throw new InvalidOperationException("Call Start() first.");

    public void Start(IEventStream stream)
    {
        // EN: Avoid spamming identical phases by applying DistinctUntilChanged.
        _phaseObs = stream
            .Observe<JsonElement>("/lol-gameflow/v1/session", withSnapshot: true)
            .Select(data => data.TryGetProperty("phase", out var p) ? p.GetString() ?? "Unknown" : "Unknown")
            .DistinctUntilChanged()
            .Select(phase => new GameflowPhaseChanged(phase));
    }
}

internal static class ObservableSelect
{
    public static IObservable<TResult> Select<T, TResult>(this IObservable<T> source, Func<T, TResult> map)
        => new SelectObservable<T, TResult>(source, map);

    private sealed class SelectObservable<T, TResult>(IObservable<T> src, Func<T, TResult> map) : IObservable<TResult>
    {
        public IDisposable Subscribe(IObserver<TResult> observer)
            => src.Subscribe(new O(map, observer));

        private sealed class O(Func<T, TResult> map, IObserver<TResult> inner) : IObserver<T>, IDisposable
        {
            private readonly Func<T, TResult> _map = map;
            private readonly IObserver<TResult> _inner = inner;
            public void OnCompleted() => _inner.OnCompleted();
            public void OnError(Exception error) => _inner.OnError(error);
            public void OnNext(T value) => _inner.OnNext(_map(value));
            public void Dispose() { }
        }
    }
}

// EN: Minimal DistinctUntilChanged operator without external dependencies.
internal static class ObservableDistinct
{
    public static IObservable<T> DistinctUntilChanged<T>(this IObservable<T> source)
        => new DistinctUntilChangedObservable<T>(source);

    private sealed class DistinctUntilChangedObservable<T>(IObservable<T> src) : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer)
            => src.Subscribe(new O(observer));

        private sealed class O(IObserver<T> inner) : IObserver<T>, IDisposable
        {
            private readonly IObserver<T> _inner = inner;
            private bool _hasLast;
            private T? _last;
            public void OnCompleted() => _inner.OnCompleted();
            public void OnError(Exception error) => _inner.OnError(error);
            public void OnNext(T value)
            {
                if (!_hasLast || !EqualityComparer<T>.Default.Equals(value, _last!))
                {
                    _last = value; _hasLast = true; _inner.OnNext(value);
                }
            }
            public void Dispose() { }
        }
    }
}
