namespace GraphQL.Subscription;

internal static class ObservableExtensions
{
    /// <summary>
    /// <para>
    /// Applies an asynchronous transformation on data events from an <see cref="IObservable{T}"/>.
    /// Maintains the order of the events produced by the <see cref="IObservable{T}"/>
    /// whether they are data, error or completion notifications.
    /// </para>
    /// <para>
    /// Ensures that after an <see cref="IObserver{T}"/> stream has been disposed,
    /// no more events will be raised (data, error or completion), and signals
    /// pending asynchronous transformations that a cancellation has been requested.
    /// </para>
    /// <para>
    /// Exceptions passed by the source through <see cref="IObserver{T}.OnError(Exception)"/> or
    /// generated by <paramref name="transformNext"/> are handled by <paramref name="transformError"/>.
    /// </para>
    /// </summary>
    public static IObservable<TOut> SelectCatchAsync<TIn, TOut>(this IObservable<TIn> observable, Func<TIn, CancellationToken, Task<TOut>> transformNext, Func<Exception, CancellationToken, Task<TOut>> transformError)
        => new SelectCatchAsyncObservable<TIn, TOut>(observable, transformNext, transformError);

    private class SelectCatchAsyncObservable<TIn, TOut> : IObservable<TOut>
    {
        private readonly IObservable<TIn> _observable;
        private readonly Func<TIn, CancellationToken, Task<TOut>> _transformNext;
        private readonly Func<Exception, CancellationToken, Task<TOut>> _transformError;

        public SelectCatchAsyncObservable(IObservable<TIn> observable, Func<TIn, CancellationToken, Task<TOut>> transformNext, Func<Exception, CancellationToken, Task<TOut>> transformError)
        {
            _observable = observable ?? throw new ArgumentNullException(nameof(observable));
            _transformNext = transformNext ?? throw new ArgumentNullException(nameof(transformNext));
            _transformError = transformError ?? throw new ArgumentNullException(nameof(transformError));
        }

        /// <summary>
        /// Subscribes to the underlying <see cref="IObservable{T}"/> with the
        /// transformation specified by this instance.
        /// <br/><br/>
        /// Disconnection requests via the returned <see cref="IDisposable"/> interface
        /// are passed to the underlying <see cref="IObservable{T}"/> and also used
        /// to signal pending asynchronous tasks that cancellation has been requested
        /// and also used to prevent further event notifications.
        /// </summary>
        public IDisposable Subscribe(IObserver<TOut> observer)
        {
            IDisposable? disposable = null;
            var newObserver = new Observer(observer, _transformNext, _transformError, () => disposable?.Dispose());
            disposable = _observable.Subscribe(newObserver);
            return newObserver;
        }

        private class Observer : IObserver<TIn>, IDisposable
        {
            private CancellationTokenSource? _cancellationTokenSource = new();
            private readonly CancellationToken _token;
            //create a queue so that events will be sent in order
            private readonly Queue<QueueData> _queue = new();
            private readonly IObserver<TOut> _observer;
            private readonly Func<TIn, CancellationToken, Task<TOut>> _transformNext;
            private readonly Func<Exception, CancellationToken, Task<TOut>> _transformError;
            private Action? _disposeAction;

            public Observer(IObserver<TOut> observer, Func<TIn, CancellationToken, Task<TOut>> transformNext, Func<Exception, CancellationToken, Task<TOut>> transformError, Action disposeAction)
            {
                _token = _cancellationTokenSource.Token;
                _observer = observer;
                _disposeAction = disposeAction;
                // ensure that the transform cannot directly throw an exception without it being wrapped in a Task<TOut>
                _transformError = async (exception, token) => await transformError(exception, token).ConfigureAwait(false);
                // if transformNext throws an exception, attempt to handle it via transformError
                _transformNext = async (data, token) =>
                {
                    try
                    {
                        return await transformNext(data, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        return await transformError(ex, token).ConfigureAwait(false);
                    }
                };
            }

            public void OnNext(TIn value) => QueueEvent(QueueType.Data, _transformNext(value, _token), null);

            public void OnError(Exception error) => QueueEvent(QueueType.Data, _transformError(error, _token), null);

            public void OnCompleted() => QueueEvent(QueueType.Completion, null, null);

            /// <summary>
            /// Queues the specified event and if necessary starts watching for an event to complete.
            /// </summary>
            private void QueueEvent(QueueType queueType, Task<TOut>? task, Exception? error)
            {
                var queueData = new QueueData { Type = queueType, Data = task, Error = error };
                bool attach = false;
                lock (_queue)
                {
                    _queue.Enqueue(queueData);
                    attach = _queue.Count == 1;
                }

                // start watching for an event to complete, if this is the first in the queue
                if (attach)
                {
                    // for data events, start sending the data once the transformation task completes
                    // but for error/completion events, send the event now
                    if (task != null)
                    {
                        // start returning data once the first task has completed (or now if already completed)
                        if (task.IsCompleted)
                        {
                            _ = ReturnDataAsync();
                        }
                        else
                        {
                            _ = task.ContinueWith(_ => ReturnDataAsync());
                        }
                    }
                    else
                        // start returning data now
                        _ = ReturnDataAsync();
                }
            }

            /// <summary>
            /// Returns data from the queue in order (or raises errors or completed notifications);
            /// executes until the queue is empty.
            /// </summary>
            private async Task ReturnDataAsync()
            {
                // grab the event at the start of the queue, but don't remove it from the queue
                QueueData? queueData;
                lock (_queue)
                {
                    // should always successfully peek from the queue here
                    queueData = _queue.Count > 0 ? _queue.Peek() : null;
                }
                while (queueData != null)
                {
                    // process the event
                    if (queueData.Type == QueueType.Data)
                    {
                        await ProcessDataAsync(queueData.Data!).ConfigureAwait(false);
                    }
                    else if (queueData.Type == QueueType.Error)
                    {
                        // currently this code cannot be hit, becuase QueueEvent is not called with QueueType.Error
                        if (!_token.IsCancellationRequested)
                            _observer.OnError(queueData.Error);
                    }
                    else if (queueData.Type == QueueType.Completion)
                    {
                        if (!_token.IsCancellationRequested)
                            _observer.OnCompleted();
                    }
                    // once the event has been passed along, dequeue it
                    lock (_queue)
                    {
                        _ = _queue.Dequeue();
                        queueData = _queue.Count > 0 ? _queue.Peek() : null;
                    }
                    // if the queue is empty, immedately quit the loop, as any new
                    // events queued will start ReturnDataAsync
                }
            }

            /// <summary>
            /// Wait for the transform to complete and push the data (or error) back to the observer.
            /// If the observer has been disposed, then data and errors are ignored.
            /// </summary>
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

            /// <summary>
            /// Disposes of the underlying observable sequence
            /// </summary>
            public void Dispose()
            {
                // cancel pending operations and prevent pending operations
                // from returning data after the observable has been detatched
                _cancellationTokenSource?.Cancel();
                // dispose the cancellation token source
                _cancellationTokenSource?.Dispose();
                // detatch the observable sequence
                _disposeAction?.Invoke();
                // release references to the degree possible
                _cancellationTokenSource = null;
                _disposeAction = null;
            }

            /// <summary>
            /// Represents an event.
            /// </summary>
            private class QueueData
            {
                /// <summary>
                /// Gets or sets the type of the event.
                /// </summary>
                public QueueType Type { get; set; }

                /// <summary>
                /// Gets or sets the <see cref="Task{TResult}"/> for a data event.
                /// </summary>
                public Task<TOut>? Data { get; set; }

                /// <summary>
                /// Gets or sets the <see cref="Exception"/> for an error event.
                /// </summary>
                public Exception? Error { get; set; }
            }

            /// <summary>
            /// The type of the event.
            /// </summary>
            private enum QueueType
            {
                Data = 0,
                Error = 1,
                Completion = 2,
            }
        }
    }
}