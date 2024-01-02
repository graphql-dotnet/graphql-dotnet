namespace GraphQL.Resolvers;

/// <summary>
/// Converts an <see cref="IObservable{T}"/> for value types into an <see cref="IObservable{T}">IObservable&lt;object?&gt;</see>.
/// </summary>
internal sealed class ObservableAdapter<T> : IObservable<object?>
{
    private readonly IObservable<T> _observable;

    public ObservableAdapter(IObservable<T> observable)
    {
        _observable = observable;
    }

    public IDisposable Subscribe(IObserver<object?> observer) => _observable.Subscribe(new ObserverAdapter(observer));

    private sealed class ObserverAdapter : IObserver<T>
    {
        private readonly IObserver<object?> _observer;
        public ObserverAdapter(IObserver<object?> observer)
        {
            _observer = observer;
        }
        public void OnCompleted() => _observer.OnCompleted();
        public void OnError(Exception error) => _observer.OnError(error);
        public void OnNext(T value) => _observer.OnNext(value); // note: boxing here
    }
}
