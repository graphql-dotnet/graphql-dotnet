using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.SystemReactive
{
    internal static class ObservableExtensions
    {
        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext, Action<Exception> onError, Action onCompleted)
            => observable.Subscribe(new Observer<T>(onNext, onError, onCompleted));

        private class Observer<T> : IObserver<T>
        {
            private readonly Action<T> _onNext;
            private readonly Action<Exception> _onError;
            private readonly Action _onCompleted;

            public Observer(Action<T> onNext, Action<Exception> onError, Action onCompleted)
            {
                _onNext = onNext;
                _onError = onError;
                _onCompleted = onCompleted;
            }

            public void OnNext(T value) => _onNext(value);

            public void OnError(Exception error) => _onError(error);

            public void OnCompleted() => _onCompleted();
        }

        public static IObservable<T> Catch<T>(this IObservable<T> observable, Func<Exception, T> transform)
            => new ObservableCatchWrapper<T>(observable, transform);

        private class ObservableCatchWrapper<T> : IObservable<T>
        {
            private readonly IObservable<T> _observable;
            private readonly Func<Exception, T> _transform;

            public ObservableCatchWrapper(IObservable<T> observable, Func<Exception, T> transform)
            {
                _observable = observable;
                _transform = transform;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return _observable.Subscribe(
                    onNext: data => observer.OnNext(data),
                    onError: error =>
                    {
                        try
                        {
                            observer.OnNext(_transform(error));
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                    onCompleted: () => observer.OnCompleted());
            }
        }

        public static IObservable<TOut> SelectAsync<TIn, TOut>(this IObservable<TIn> observable, Func<TIn, CancellationToken, Task<TOut>> func)
            => new SelectAsyncObservable<TIn, TOut>(observable, func);

        private class SelectAsyncObservable<TIn, TOut> : IObservable<TOut>
        {
            private readonly IObservable<TIn> _observable;
            private readonly Func<TIn, CancellationToken, Task<TOut>> _transform;

            public SelectAsyncObservable(IObservable<TIn> observable, Func<TIn, CancellationToken, Task<TOut>> transform)
            {
                _observable = observable;
                _transform = transform;
            }

            public IDisposable Subscribe(IObserver<TOut> observer)
            {
                IDisposable? disposable = null;
                var newObserver = new SelectAsyncObserver<TIn, TOut>(observer, _transform, () => disposable?.Dispose());
                disposable = _observable.Subscribe(newObserver);
                return newObserver;
            }
        }

        private class SelectAsyncObserver<TIn, TOut> : IObserver<TIn>, IDisposable
        {
            private CancellationTokenSource? _cancellationTokenSource = new();
            private readonly CancellationToken _token;
            //create a queue so that events will be sent in order
            private readonly object _sync = new();
            private readonly Queue<QueueData> _queue = new();
            private readonly IObserver<TOut> _observer;
            private readonly Func<TIn, CancellationToken, Task<TOut>> _transform;
            private Action? _disposeAction;

            public SelectAsyncObserver(IObserver<TOut> observer, Func<TIn, CancellationToken, Task<TOut>> transform, Action disposeAction)
            {
                _token = _cancellationTokenSource.Token;
                _observer = observer;
                //ensure that the transform cannot directly throw an exception without it being wrapped in a Task<TOut>
                _transform = async (data, token) => await transform(data, token).ConfigureAwait(false);
                _disposeAction = disposeAction;
            }

            public void OnNext(TIn value) => QueueEvent(QueueType.Data, _transform(value, _token), null);

            public void OnError(Exception error) => QueueEvent(QueueType.Error, null, error);

            public void OnCompleted() => QueueEvent(QueueType.Completion, null, null);

            //queues the specified event and if necessary starts watching for an event to complete
            private void QueueEvent(QueueType queueType, Task<TOut>? task, Exception? error)
            {
                var queueData = new QueueData { Type = queueType, Data = task, Error = error };
                bool attach = false;
                lock (_sync)
                {
                    _queue.Enqueue(queueData);
                    attach = _queue.Count == 1;
                }
                //start watching for an event to complete, if this is the first in the queue
                if (attach)
                {
                    if (task != null)
                        //start returning data once the first task has completed (or now if already completed)
                        _ = task.ContinueWith(ReturnDataAsync);
                    else
                        //start returning data now
                        _ = ReturnDataAsync(null);
                }
            }

            //returns data from the queue in order (or raises errors or completed notifications)
            //executes until the queue is empty
            private async Task ReturnDataAsync(Task<TOut>? dummy)
            {
                QueueData? queueData;
                lock (_sync)
                {
                    queueData = _queue.Count > 0 ? _queue.Peek() : null;
                }
                while (queueData != null)
                {
                    if (queueData.Type == QueueType.Data)
                    {
                        await ProcessDataAsync(queueData.Data!).ConfigureAwait(false);
                    }
                    else if (queueData.Type == QueueType.Error)
                    {
                        _observer.OnError(queueData.Error);
                    }
                    else if (queueData.Type == QueueType.Completion)
                    {
                        _observer.OnCompleted();
                    }
                    lock (_sync)
                    {
                        _ = _queue.Dequeue();
                        queueData = _queue.Count > 0 ? _queue.Peek() : null;
                    }
                }
            }

            //waits for the transform to complete and pushes the data (or error) back to the observer
            //if the observer has been disposed, then data and errors are ignored
            private async Task ProcessDataAsync(Task<TOut> dataTask)
            {
                TOut dataOut;
                try
                {
                    dataOut = await dataTask.ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    if (!_token.IsCancellationRequested)
                        _observer.OnError(error);
                    return;
                }
                if (!_token.IsCancellationRequested)
                    _observer.OnNext(dataOut);
            }

            public void Dispose()
            {
                //cancel pending operations and prevent pending operations
                //from returning data after the observable has been detatched
                _cancellationTokenSource?.Cancel();
                //dispose the cancellation token source
                _cancellationTokenSource?.Dispose();
                //detatch the observable sequence
                _disposeAction?.Invoke();
                //release references to the degree possible
                _cancellationTokenSource = null;
                _disposeAction = null;
            }

            private class QueueData
            {
                public QueueType Type;
                public Task<TOut>? Data;
                public Exception? Error;
            }

            private enum QueueType
            {
                Data = 0,
                Error = 1,
                Completion = 2,
            }
        }
    }
}
