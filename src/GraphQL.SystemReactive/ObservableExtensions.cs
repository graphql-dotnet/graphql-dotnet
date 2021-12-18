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

        public static IObservable<TOut> Select<TIn, TOut>(this IObservable<TIn> observable, Func<TIn, TOut> transform)
            => new ObservableSelectWrapper<TIn, TOut>(observable, transform);

        private class ObservableSelectWrapper<TIn, TOut> : IObservable<TOut>
        {
            private readonly IObservable<TIn> _observable;
            private readonly Func<TIn, TOut> _transform;

            public ObservableSelectWrapper(IObservable<TIn> observable, Func<TIn, TOut> transform)
            {
                _observable = observable;
                _transform = transform;
            }

            public IDisposable Subscribe(IObserver<TOut> observer)
            {
                return _observable.Subscribe(
                    onNext: data => observer.OnNext(_transform(data)),
                    onError: error => observer.OnError(error),
                    onCompleted: () => observer.OnCompleted());
            }
        }

        public static IObservable<TOut> SelectAsync<TIn, TOut>(this IObservable<TIn> observable, Func<TIn, CancellationToken, Task<TOut>> func)
            => new ObservableSelectAsyncWrapper<TIn, TOut>(observable, func);

        private class ObservableSelectAsyncWrapper<TIn, TOut> : IObservable<TOut>
        {
            private readonly IObservable<TIn> _observable;
            private readonly Func<TIn, CancellationToken, Task<TOut>> _transform;

            public ObservableSelectAsyncWrapper(IObservable<TIn> observable, Func<TIn, CancellationToken, Task<TOut>> transform)
            {
                _observable = observable;
                //ensure that the transform cannot directly throw an exception without it being wrapped in a Task<TOut>
                _transform = async (data, token) => await transform(data, token).ConfigureAwait(false);
            }

            public IDisposable Subscribe(IObserver<TOut> observer)
            {
                //prevent async tasks from triggering after an observable sequence has been disposed
                CancellationTokenSource? cancellationTokenSource = new();
                var token = cancellationTokenSource.Token;
                //create a queue so that events will be sent in order
                object sync = new();
                Queue<QueueData> queue = new();

                //subscribe to the underlying IObservable, and queue each event
                var disposable = _observable.Subscribe(
                    onNext: dataIn => QueueEvent(QueueType.Data, _transform(dataIn, token), null),
                    onError: error => QueueEvent(QueueType.Error, null, error),
                    onCompleted: () => QueueEvent(QueueType.Completion, null, null));

                //queues the specified event and if necessary starts watching for an event to complete
                void QueueEvent(QueueType queueType, Task<TOut>? task, Exception? error)
                {
                    var queueData = new QueueData { Type = queueType, Data = task, Error = error };
                    bool attach = false;
                    lock (sync)
                    {
                        queue.Enqueue(queueData);
                        attach = queue.Count == 1;
                    }
                    //start watching for an event to complete, if this is the first in the queue
                    if (attach)
                    {
                        if (task != null)
                            //start returning data once the first task has completed (or now if already completed)
                            _ = task.ContinueWith(ReturnData);
                        else
                            //start returning data now
                            _ = ReturnData(null);
                    }
                }

                //returns data from the queue in order (or raises errors or completed notifications)
                //executes until the queue is empty
                async Task ReturnData(Task<TOut>? dummy)
                {
                    QueueData? queueData;
                    lock (sync)
                    {
                        queueData = queue.Count > 0 ? queue.Peek() : null;
                    }
                    while (queueData != null)
                    {
                        if (queueData.Type == QueueType.Data)
                        {
                            await ProcessData(queueData.Data!).ConfigureAwait(false);
                        }
                        else if (queueData.Type == QueueType.Error)
                        {
                            observer.OnError(queueData.Error);
                        }
                        else if (queueData.Type == QueueType.Completion)
                        {
                            observer.OnCompleted();
                        }
                        lock (sync)
                        {
                            _ = queue.Dequeue();
                            queueData = queue.Count > 0 ? queue.Peek() : null;
                        }
                    }
                }

                //waits for the transform to complete and pushes the data (or error) back to the observer
                //if the observer has been disposed, then data and errors are ignored
                async Task ProcessData(Task<TOut> dataTask)
                {
                    TOut dataOut;
                    try
                    {
                        dataOut = await dataTask.ConfigureAwait(false);
                    }
                    catch (Exception error)
                    {
                        if (!token.IsCancellationRequested)
                            observer.OnError(error);
                        return;
                    }
                    if (!token.IsCancellationRequested)
                        observer.OnNext(dataOut);
                }

                //returns a disconnection 'delegate' that disposes the underlying observer,
                //prevents any asynchronous tasks from returning data,
                //and signals cancellation to any pending tasks
                //note: pending tasks will execute to completion but will ignore data and transformation errors
                return new Disposable(() =>
                {
                    //cancel pending operations and prevent pending operations
                    //from returning data after the observable has been detatched
                    cancellationTokenSource?.Cancel();
                    //dispose the cancellation token source
                    cancellationTokenSource?.Dispose();
                    //detatch the observable sequence
                    disposable?.Dispose();
                    //release references to the degree possible
                    cancellationTokenSource = null;
                    disposable = null;
                });
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

        private class Disposable : IDisposable
        {
            private readonly Action _action;

            public Disposable(Action action)
            {
                _action = action;
            }

            public void Dispose() => _action();
        }
    }
}
