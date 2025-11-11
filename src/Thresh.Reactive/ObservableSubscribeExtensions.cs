// src/Thresh.Reactive/ObservableSubscribeExtensions.cs
using System;

namespace Thresh.Reactive
{
    /// <summary>
    /// Minimal Subscribe overloads so you can pass lambdas without System.Reactive.
    /// </summary>
    public static class ObservableSubscribeExtensions
    {
        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext)
            => source.Subscribe(new AnonymousObserver<T>(onNext, _ => { }, () => { }));

        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError)
            => source.Subscribe(new AnonymousObserver<T>(onNext, onError, () => { }));

        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted)
            => source.Subscribe(new AnonymousObserver<T>(onNext, onError, onCompleted));

        private sealed class AnonymousObserver<T> : IObserver<T>
        {
            private readonly Action<T> _onNext;
            private readonly Action<Exception> _onError;
            private readonly Action _onCompleted;

            public AnonymousObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
            {
                _onNext = onNext ?? (_ => { });
                _onError = onError ?? (_ => { });
                _onCompleted = onCompleted ?? (() => { });
            }

            public void OnNext(T value) => _onNext(value);
            public void OnError(Exception error) => _onError(error);
            public void OnCompleted() => _onCompleted();
        }
    }
}
