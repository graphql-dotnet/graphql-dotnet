#nullable enable


namespace GraphQL.Tests.Subscription;

internal class SampleObservable<T> : IObservable<T>
{
    private readonly List<IObserver<T>> _observers = new List<IObserver<T>>();

    public void Next(T data)
    {
        IObserver<T>[] observers;
        lock (_observers)
        {
            observers = _observers.ToArray();
        }
        foreach (var observer in observers)
        {
            observer.OnNext(data);
        }
    }

    public void Error(Exception exception)
    {
        IObserver<T>[] observers;
        lock (_observers)
        {
            observers = _observers.ToArray();
        }
        foreach (var observer in observers)
        {
            observer.OnError(exception);
        }
    }

    public void Completed()
    {
        IObserver<T>[] observers;
        lock (_observers)
        {
            observers = _observers.ToArray();
        }
        foreach (var observer in observers)
        {
            observer.OnCompleted();
        }
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (_observers)
        {
            _observers.Add(observer);
        }
        return new Disposer(this, observer);
    }

    private class Disposer : IDisposable
    {
        private SampleObservable<T>? _source;
        private IObserver<T>? _observer;

        public Disposer(SampleObservable<T> source, IObserver<T> observer)
        {
            _source = source;
            _observer = observer;
        }

        public void Dispose()
        {
            var source = Interlocked.Exchange(ref _source, null);
            if (source == null)
                return;
            var observer = Interlocked.Exchange(ref _observer, null);
            lock (source._observers)
            {
                source._observers.Remove(observer!);
            }
        }
    }
}
