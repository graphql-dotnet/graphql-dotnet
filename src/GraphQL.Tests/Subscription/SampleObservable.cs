#nullable enable

namespace GraphQL.Tests.Subscription;

internal class SampleObservable<T> : IObservable<T>
{
    private readonly List<IObserver<T>> _observers = new();

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
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
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
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));
        lock (_observers)
        {
            _observers.Add(observer);
        }
        return new Unsubscriber(this, observer);
    }

    private class Unsubscriber : IDisposable
    {
        private SampleObservable<T>? _source;
        private IObserver<T>? _observer;

        public Unsubscriber(SampleObservable<T> source, IObserver<T> observer)
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
