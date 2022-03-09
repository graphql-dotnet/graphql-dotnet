#nullable enable

using GraphQL.Subscription;

namespace GraphQL.Tests.Subscription;

public class ObservableTests : IDisposable
{
    private MockObservable<string> Source { get; } = new();
    private MockObserver Observer { get; } = new();

    public void Dispose() => Observer.Dispose();

    [Fact]
    public async Task DataInOrder()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s);
                    return data;
                },
                (error, token) => Task.FromResult(error.Message));
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Next("0");
        Source.Error(new Exception("abc"));
        Source.Next("300");
        Source.Completed();
        await Observer.WaitForAsync("Next '200'. Next '0'. Next 'abc'. Next '300'. Completed. ");
    }

    [Fact]
    public async Task ExceptionsInDataTransformAreTransformed_Async()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s);
                    return data;
                },
                (error, token) => Task.FromResult(error.GetType().Name));
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Next("aa");
        await Observer.WaitForAsync("Next '200'. Next 'FormatException'. ");
    }

    [Fact]
    public async Task ExceptionsInDataTransformAreTransformed_Sync()
    {
        var observable = Source
            .SelectCatchAsync(
                (data, token) =>
                {
                    var s = int.Parse(data);
                    Thread.Sleep(s);
                    return Task.FromResult(data);
                },
                (error, token) => Task.FromResult(error.GetType().Name));
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Next("aa");
        await Observer.WaitForAsync("Next '200'. Next 'FormatException'. ");
    }

    [Fact]
    public async Task ExceptionsInErrorTransformArePassedThrough_Async()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s);
                    return data;
                },
                (error, token) => Task.FromException<string>(error));
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Next("aa");
        await Observer.WaitForAsync("Next '200'. Error 'FormatException'. ");
    }

    [Fact]
    public async Task ExceptionsInErrorTransformArePassedThrough_Sync()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s);
                    return data;
                },
                (error, token) => throw error);
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Next("aa");
        await Observer.WaitForAsync("Next '200'. Error 'FormatException'. ");
    }

    [Fact]
    public async Task ImmediateSend()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s);
                    return data;
                },
                (error, token) => throw error);
        observable.Subscribe(Observer);
        Source.Completed();
        Source.Next("0");
        Source.Next("200");
        await Observer.WaitForAsync("Completed. Next '0'. Next '200'. ");
    }

    [Fact]
    public async Task SendPauseSend()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s);
                    return data;
                },
                (error, token) => throw error);
        observable.Subscribe(Observer);
        Source.Next("10");
        await Task.Delay(500);
        Source.Next("20");
        await Observer.WaitForAsync("Next '10'. Next '20'. ");
    }

    [Fact]
    public void SendSynchronously()
    {
        // validates that if the transformations execute synchronously,
        // nothing gets scheduled on the task scheduler
        var observable = Source
            .SelectCatchAsync(
                (data, token) => Task.FromResult(data),
                (error, token) => error is ApplicationException ? throw error : Task.FromResult(error.GetType().Name));
        observable.Subscribe(Observer);
        Source.Next("a");
        Source.Error(new ApplicationException());
        Source.Next("b");
        Source.Error(new InvalidTimeZoneException());
        Source.Completed();
        Observer.Current.ShouldBe("Next 'a'. Error 'ApplicationException'. Next 'b'. Next 'InvalidTimeZoneException'. Completed. ");
    }

    [Fact]
    public async Task CanceledSubscriptionsDontSendData()
    {
        var observable = Source
            .SelectCatchAsync(
                (data, token) => Task.FromResult(data),
                (error, token) => error is ApplicationException ? throw error : Task.FromResult(error.GetType().Name));
        var disposer = observable.Subscribe(Observer);
        Source.Next("test");
        Observer.Current.ShouldBe("Next 'test'. ");
        disposer.Dispose();
        Source.Next("a");
        Source.Error(new ApplicationException());
        Source.Next("b");
        Source.Error(new InvalidTimeZoneException());
        Source.Next("c");
        Source.Completed();
        await Task.Delay(200); // just in case, but should execute synchronously anyway
        Observer.Current.ShouldBe("Next 'test'. ");
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        var observableSuccess = Source.SelectCatchAsync<string, string>((_, _) => null!, (_, _) => null!);
        Should.Throw<ArgumentNullException>(() =>
        {
            var observableFail = Source.SelectCatchAsync<string, string>(null!, (_, _) => null!);
        });
        Should.Throw<ArgumentNullException>(() =>
        {
            var observableFail = Source.SelectCatchAsync<string, string>((_, _) => null!, null!);
        });
        Should.Throw<ArgumentNullException>(() =>
        {
            var observableFail = ((IObservable<string>)null!).SelectCatchAsync<string, string>((_, _) => null!, (_, _) => null!);
        });
    }

    private class MockObserver : IObserver<string>, IDisposable
    {
        private readonly System.Text.StringBuilder _stringBuilder = new();
        private string? _expected;
        private readonly SemaphoreSlim _received = new(0, 1);

        public string Current
        {
            get
            {
                lock (_stringBuilder)
                    return _stringBuilder.ToString();
            }
        }

        public async Task WaitForAsync(string expected)
        {
            lock (_stringBuilder)
            {
                _expected = expected;
                if (_stringBuilder.ToString() == _expected)
                    return;
            }

            if (await _received.WaitAsync(10000))
                return;

            lock (_stringBuilder)
            {
                _stringBuilder.ToString().ShouldBe(expected);
            }
        }

        public void OnNext(string value)
        {
            lock (_stringBuilder)
            {
                _stringBuilder.Append($"Next '{value}'. ");
                if (_stringBuilder.ToString() == _expected)
                    _received.Release();
            }
        }

        public void OnError(Exception exception)
        {
            lock (_stringBuilder)
            {
                _stringBuilder.Append($"Error '{exception.GetType().Name}'. ");
                if (_stringBuilder.ToString() == _expected)
                    _received.Release();
            }
        }

        public void OnCompleted()
        {
            lock (_stringBuilder)
            {
                _stringBuilder.Append($"Completed. ");
                if (_stringBuilder.ToString() == _expected)
                    _received.Release();
            }
        }

        public void Dispose()
        {
            _received.Dispose();
        }
    }

    private class MockObservable<T> : IObservable<T>
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
            private MockObservable<T>? _source;
            private IObserver<T>? _observer;

            public Disposer(MockObservable<T> source, IObserver<T> observer)
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
}
