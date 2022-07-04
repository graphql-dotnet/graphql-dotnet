#nullable enable
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace GraphQL.Tests.Subscription;

public class ObservableExtensionsTests
{
    private SampleObservable<string> Source { get; } = new();
    private SampleObserver Observer { get; } = new();

    [Fact]
    public async Task DataInOrder()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s).ConfigureAwait(false);
                    return data;
                },
                async (error, token) =>
                {
                    error.Message.ShouldBe("abc");
                    return new ApplicationException();
                });
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Next("0");
        Source.Error(new Exception("abc"));
        Source.Next("300");
        Source.Completed();
        await Observer.WaitForAsync("Next '200'. Next '0'. Error 'ApplicationException'. Next '300'. Completed. ").ConfigureAwait(false);
    }

    [Fact]
    public async Task ExceptionsInDataTransformAreTransformed_Async()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s).ConfigureAwait(false);
                    return data;
                },
                (error, token) => throw new NotSupportedException());
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Next("aa");
        await Observer.WaitForAsync("Next '200'. Error 'FormatException'. ").ConfigureAwait(false);
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
                    return new ValueTask<string>(data);
                },
                (error, token) => throw new NotSupportedException());
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Next("aa");
        await Observer.WaitForAsync("Next '200'. Error 'FormatException'. ").ConfigureAwait(false);
    }

    [Fact]
    public async Task ExceptionsInErrorTransformArePassedThrough_Async()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s).ConfigureAwait(false);
                    return data;
                },
                async (error, token) =>
                {
                    await Task.Delay(200).ConfigureAwait(false);
                    return new FormatException();
                });
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Error(new ApplicationException());
        await Observer.WaitForAsync("Next '200'. Error 'FormatException'. ").ConfigureAwait(false);
    }

    [Fact]
    public async Task ExceptionsInErrorTransformArePassedThrough_Sync()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s).ConfigureAwait(false);
                    return data;
                },
                (error, token) => throw new FormatException());
        observable.Subscribe(Observer);
        Source.Next("200");
        Source.Error(new ApplicationException());
        await Observer.WaitForAsync("Next '200'. Error 'FormatException'. ").ConfigureAwait(false);
    }

    [Fact]
    public async Task ImmediateSend()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s).ConfigureAwait(false);
                    return data;
                },
                (error, token) => throw error);
        observable.Subscribe(Observer);
        Source.Completed();
        Source.Next("0");
        Source.Next("200");
        await Observer.WaitForAsync("Completed. Next '0'. Next '200'. ").ConfigureAwait(false);
    }

    [Fact]
    public async Task SendPauseSend()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s).ConfigureAwait(false);
                    return data;
                },
                (error, token) => throw error);
        observable.Subscribe(Observer);
        Source.Next("10");
        await Task.Delay(500).ConfigureAwait(false);
        Source.Next("20");
        await Observer.WaitForAsync("Next '10'. Next '20'. ").ConfigureAwait(false);
    }

    [Fact]
    public void SendSynchronously()
    {
        // validates that if the transformations execute synchronously,
        // nothing gets scheduled on the task scheduler
        var observable = Source
            .SelectCatchAsync(
                (data, token) => new ValueTask<string>(data),
                (error, token) => error is ApplicationException ? throw error : new ValueTask<Exception>(new DivideByZeroException()));
        observable.Subscribe(Observer);
        Source.Next("a");
        Source.Error(new ApplicationException());
        Source.Next("b");
        Source.Error(new InvalidTimeZoneException());
        Source.Completed();
        Observer.Current.ShouldBe("Next 'a'. Error 'ApplicationException'. Next 'b'. Error 'DivideByZeroException'. Completed. ");
    }

    [Fact]
    public async Task CanceledSubscriptionsDontSendData()
    {
        var observable = Source
            .SelectCatchAsync(
                (data, token) => new ValueTask<string>(data),
                (error, token) => error is ApplicationException ? throw error : new ValueTask<Exception>(new DivideByZeroException()));
        var subscription = observable.Subscribe(Observer);
        Source.Next("test");
        Observer.Current.ShouldBe("Next 'test'. ");
        subscription.Dispose();
        Source.Next("a");
        Source.Error(new ApplicationException());
        Source.Next("b");
        Source.Error(new InvalidTimeZoneException());
        Source.Next("c");
        Source.Completed();
        await Task.Delay(200).ConfigureAwait(false); // just in case, but should execute synchronously anyway
        Observer.Current.ShouldBe("Next 'test'. ");
    }

    [Fact]
    public async Task CanceledSubscriptionsDontSendPendingData()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    var s = int.Parse(data);
                    await Task.Delay(s).ConfigureAwait(false);
                    return data;
                },
                (error, token) => new ValueTask<Exception>(error));
        var subscription = observable.Subscribe(Observer);
        Source.Next("200"); // if the value is 0, it completes synchronously, and the test would fail
        Source.Next("200"); // another asynchronous event
        Source.Error(new ExecutionError("test")); // a completed synchronous transformation, but in the queue after one with a delay
        subscription.Dispose();
        Observer.Current.ShouldBe("");
        await Task.Delay(1000).ConfigureAwait(false);
        Observer.Current.ShouldBe("");
    }

    [Fact]
    public async Task CanceledSubscriptionsDontTransform()
    {
        bool transformed = false;
        bool disposed = false;
        var observable = Source
            .SelectCatchAsync(
                async (data, token) =>
                {
                    transformed = disposed;
                    return data;
                },
                async (error, token) =>
                {
                    transformed = disposed;
                    return error;
                });
        var subscription = observable.Subscribe(Observer);
        Source.Next("test");
        Source.Error(new DivideByZeroException());
        Observer.Current.ShouldBe("Next 'test'. Error 'DivideByZeroException'. ");
        subscription.Dispose();
        disposed = true;
        Source.Next("a");
        Source.Error(new ApplicationException());
        Source.Next("b");
        Source.Error(new InvalidTimeZoneException());
        Source.Next("c");
        Source.Completed();
        await Task.Delay(200).ConfigureAwait(false); // just in case, but should execute synchronously anyway
        Observer.Current.ShouldBe("Next 'test'. Error 'DivideByZeroException'. ");
        transformed.ShouldBeFalse();
    }

    [Fact]
    public void CanCallDisposeTwice()
    {
        var observable = Source
            .SelectCatchAsync(
                async (data, token) => data,
                async (error, token) => error);
        var subscription = observable.Subscribe(Observer);
        subscription.Dispose();
        subscription.Dispose();
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        var observableSuccess = Source.SelectCatchAsync<string, string>((_, _) => default, (_, _) => default);
        Should.Throw<ArgumentNullException>(() =>
        {
            var observableFail = Source.SelectCatchAsync<string, string>(null!, (_, _) => default);
        });
        Should.Throw<ArgumentNullException>(() =>
        {
            var observableFail = Source.SelectCatchAsync<string, string>((_, _) => default, default!);
        });
        Should.Throw<ArgumentNullException>(() =>
        {
            var observableFail = ((IObservable<string>)null!).SelectCatchAsync<string, string>((_, _) => default, (_, _) => default);
        });
    }

    private class SampleObserver : IObserver<string>
    {
        private readonly System.Text.StringBuilder _stringBuilder = new();
        private string? _expected;
        private readonly TaskCompletionSource<bool> _received = new();

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

            var completedTask = await Task.WhenAny(_received.Task, Task.Delay(30000)).ConfigureAwait(false);

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
                    _received.TrySetResult(true);
            }
        }

        public void OnError(Exception exception)
        {
            lock (_stringBuilder)
            {
                _stringBuilder.Append($"Error '{exception.GetType().Name}'. ");
                if (_stringBuilder.ToString() == _expected)
                    _received.TrySetResult(true);
            }
        }

        public void OnCompleted()
        {
            lock (_stringBuilder)
            {
                _stringBuilder.Append($"Completed. ");
                if (_stringBuilder.ToString() == _expected)
                    _received.TrySetResult(true);
            }
        }
    }
}
