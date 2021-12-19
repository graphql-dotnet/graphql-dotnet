using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.SystemReactive;
using Moq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Subscription
{
    public class ObservableTests
    {
        [Fact]
        public void CancellationTokensWorkAsExpected()
        {
            //create a cancellation token source
            var cts = new CancellationTokenSource();
            //grab the token
            var token = cts.Token;
            //signal tokens that they are canceled (performed synchronously, per docs)
            cts.Cancel();
            //dispose of the cancellation token source
            cts.Dispose();
            //at this point the token is still valid
            bool executed = false;
            //attempting to register a callback should immediately run the callback because the token is canceled,
            //pursuant to MS docs. note that the docs also say that this can throw an InvalidOperationException
            //if the source is disposed, but since the token has already been canceled, we should be fine
            token.Register(() => executed = true).Dispose();
            //the callback should run synchronously (per docs), so executed should equal true immediately
            executed.ShouldBeTrue();
        }

        private class EnforcedOrderWaiter : IDisposable
        {
            private readonly List<SemaphoreSlim> _semaphores = new();

            public EnforcedOrderWaiter(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    _semaphores.Add(new SemaphoreSlim(0, 1));
                }
                _semaphores[0].Release();
            }

            public Task WaitForSequence(int value)
                => WaitForSequence(value, () => { });

            public async Task WaitForSequence(int value, Action beforeNext)
            {
                await Task.Delay(200);
                if (await _semaphores[value - 1].WaitAsync(15000) == false)
                    throw new Exception($"Timeout while waiting for sequence {value}");
                beforeNext();
                if (value < _semaphores.Count)
                {
                    Action next = async () =>
                    {
                        await Task.Delay(200);
                        _semaphores[value].Release();
                    };
                    next();
                }
            }

            public void Dispose()
            {
                foreach (var semaphore in _semaphores)
                    semaphore.Dispose();
            }
        }

        [Fact]
        public async Task SelectAsync_DataArrivesInOrder()
        {
            using var order = new EnforcedOrderWaiter(3);
            IObserver<string> observer = null;
            bool disposed = false;
            var disposableMock = new Mock<IDisposable>(MockBehavior.Strict);
            disposableMock.Setup(x => x.Dispose()).Callback(() => disposed = true);
            var observableMock = new Mock<IObservable<string>>(MockBehavior.Strict);
            observableMock.Setup(x => x.Subscribe(It.IsAny<IObserver<string>>())).Returns<IObserver<string>>(observer2 =>
            {
                observer = observer2;
                return disposableMock.Object;
            });
            var finishedEvents = new List<string>();
            var mappedObservable = observableMock.Object.SelectAsync(async (value, token) =>
            {
                await order.WaitForSequence(int.Parse(value), () => finishedEvents.Add(value));
                return value;
            });
            var events = new List<string>();
            var baseObserverMock = new Mock<IObserver<string>>(MockBehavior.Strict);
            baseObserverMock.Setup(x => x.OnNext(It.IsAny<string>())).Callback<string>(value =>
            {
                lock (events)
                    events.Add($"OnNext: {value}");
            });
            baseObserverMock.Setup(x => x.OnError(It.IsAny<Exception>())).Callback<Exception>(value =>
            {
                lock (events)
                    events.Add($"OnError: {value.GetType().Name}");
            });
            baseObserverMock.Setup(x => x.OnCompleted()).Callback(() =>
            {
                lock (events)
                    events.Add("OnCompleted");
            });
            var mappedDisposable = mappedObservable.Subscribe(baseObserverMock.Object);
            observer.ShouldNotBeNull();
            disposed.ShouldBeFalse();
            //these next events are all triggered immediately, but may cause a delay during SelectAsync
            observer.OnNext("2"); // 1-second delay
            observer.OnError(new InvalidTimeZoneException()); //error is thrown
            observer.OnNext("1"); // 0.5-second delay
            observer.OnCompleted(); // completion
            await order.WaitForSequence(3);
            await WaitForEvents(events, 4);
            mappedDisposable.Dispose();
            disposed.ShouldBeTrue();
            //verify events were raised in order
            events.ShouldBe(new string[]
            {
                "OnNext: 2",
                "OnError: InvalidTimeZoneException",
                "OnNext: 1",
                "OnCompleted",
            });
            //verify that async operations were completed out of order
            finishedEvents.ShouldBe(new string[]
            {
                "1",
                "2",
            });
        }

        [Fact]
        public async Task SelectAsync_MultipleSequences()
        {
            using var order = new EnforcedOrderWaiter(5);
            IObserver<string> observer = null;
            bool disposed = false;
            var disposableMock = new Mock<IDisposable>(MockBehavior.Strict);
            disposableMock.Setup(x => x.Dispose()).Callback(() => disposed = true);
            var observableMock = new Mock<IObservable<string>>(MockBehavior.Strict);
            observableMock.Setup(x => x.Subscribe(It.IsAny<IObserver<string>>())).Returns<IObserver<string>>(observer2 =>
            {
                observer = observer2;
                return disposableMock.Object;
            });
            var finishedEvents = new List<string>();
            var mappedObservable = observableMock.Object.SelectAsync(async (value, token) =>
            {
                await order.WaitForSequence(int.Parse(value), () => finishedEvents.Add(value));
                return value;
            });
            var events = new List<string>();
            var baseObserverMock = new Mock<IObserver<string>>(MockBehavior.Strict);
            baseObserverMock.Setup(x => x.OnNext(It.IsAny<string>())).Callback<string>(value =>
            {
                lock (events)
                    events.Add($"OnNext: {value}");
            });
            baseObserverMock.Setup(x => x.OnError(It.IsAny<Exception>())).Callback<Exception>(value =>
            {
                lock (events)
                    events.Add($"OnError: {value.GetType().Name}");
            });
            baseObserverMock.Setup(x => x.OnCompleted()).Callback(() =>
            {
                lock (events)
                    events.Add("OnCompleted");
            });
            var mappedDisposable = mappedObservable.Subscribe(baseObserverMock.Object);
            observer.ShouldNotBeNull();
            disposed.ShouldBeFalse();
            observer.OnNext("2");
            observer.OnNext("1");
            await WaitForEvents(events, 2);
            observer.OnNext("4");
            observer.OnNext("3");
            await WaitForEvents(events, 4);
            observer.OnCompleted();
            await WaitForEvents(events, 5);
            mappedDisposable.Dispose();
            disposed.ShouldBeTrue();
            //verify events were raised in order queued
            events.ShouldBe(new string[]
            {
                "OnNext: 2",
                "OnNext: 1",
                "OnNext: 4",
                "OnNext: 3",
                "OnCompleted",
            });
            //verify that async operations were completed out of the order they were queued
            finishedEvents.ShouldBe(new string[]
            {
                "1",
                "2",
                "3",
                "4",
            });
        }

        private async Task WaitForEvents(List<string> events, int count)
        {
            var expire = DateTimeOffset.UtcNow.AddSeconds(15);
            while (DateTimeOffset.UtcNow < expire)
            {
                await Task.Delay(200);
                lock (events)
                {
                    if (events.Count == count)
                        return;
                }
            }
            throw new Exception($"Timeout waiting for events count == {count}");
        }

        [Fact]
        public async Task SelectAsync_NoEventsProducedAfterDispose()
        {
            using var order = new EnforcedOrderWaiter(6);
            //also verifies that cancellation token passed to SelectAsync delegate works properly
            IObserver<string> observer = null;
            bool disposed = false;
            var disposableMock = new Mock<IDisposable>(MockBehavior.Strict);
            disposableMock.Setup(x => x.Dispose()).Callback(() => disposed = true);
            var observableMock = new Mock<IObservable<string>>(MockBehavior.Strict);
            observableMock.Setup(x => x.Subscribe(It.IsAny<IObserver<string>>())).Returns<IObserver<string>>(observer2 =>
            {
                observer = observer2;
                return disposableMock.Object;
            });
            var canceledEvents = new List<string>();
            var mappedObservable = observableMock.Object.SelectAsync(async (value, token) =>
            {
                await order.WaitForSequence(int.Parse(value));
                if (token.IsCancellationRequested)
                {
                    lock (canceledEvents)
                        canceledEvents.Add(value);
                }
                return value;
            });
            var events = new List<string>();
            var baseObserverMock = new Mock<IObserver<string>>(MockBehavior.Strict);
            baseObserverMock.Setup(x => x.OnNext(It.IsAny<string>())).Callback<string>(value => events.Add($"OnNext: {value}"));
            baseObserverMock.Setup(x => x.OnError(It.IsAny<Exception>())).Callback<Exception>(value => events.Add($"OnError: {value.GetType().Name}"));
            baseObserverMock.Setup(x => x.OnCompleted()).Callback(() => events.Add("OnCompleted"));
            var mappedDisposable = mappedObservable.Subscribe(baseObserverMock.Object);
            observer.ShouldNotBeNull();
            disposed.ShouldBeFalse();
            observer.OnNext("1");
            observer.OnNext("5");
            observer.OnError(new InvalidTimeZoneException());
            observer.OnNext("2");
            observer.OnCompleted();
            await order.WaitForSequence(3);
            await WaitForEvents(events, 1);
            await Task.Delay(1000);
            mappedDisposable.Dispose();
            await order.WaitForSequence(4);
            disposed.ShouldBeTrue();
            await order.WaitForSequence(6);
            await WaitForEvents(canceledEvents, 1);
            events.ShouldBe(new string[]
            {
                "OnNext: 1",
            });
            canceledEvents.ShouldBe(new string[]
            {
                "5",
            });
        }

        [Fact]
        public void Catch()
        {
            IObserver<string> observer = null;
            bool disposed = false;
            var disposableMock = new Mock<IDisposable>(MockBehavior.Strict);
            disposableMock.Setup(x => x.Dispose()).Callback(() => disposed = true);
            var observableMock = new Mock<IObservable<string>>(MockBehavior.Strict);
            observableMock.Setup(x => x.Subscribe(It.IsAny<IObserver<string>>())).Returns<IObserver<string>>(observer2 =>
            {
                observer = observer2;
                return disposableMock.Object;
            });
            var mappedObservable = observableMock.Object.Catch(error => $"Error caught: {error.GetType().Name}");
            var events = new List<string>();
            var baseObserverMock = new Mock<IObserver<string>>(MockBehavior.Strict);
            baseObserverMock.Setup(x => x.OnNext(It.IsAny<string>())).Callback<string>(value => events.Add($"OnNext: {value}"));
            baseObserverMock.Setup(x => x.OnError(It.IsAny<Exception>())).Callback<Exception>(value => events.Add($"OnError: {value.GetType().Name}"));
            baseObserverMock.Setup(x => x.OnCompleted()).Callback(() => events.Add("OnCompleted"));
            var mappedDisposable = mappedObservable.Subscribe(baseObserverMock.Object);
            observer.ShouldNotBeNull();
            disposed.ShouldBeFalse();
            observer.OnNext("Event1");
            observer.OnError(new InvalidTimeZoneException());
            observer.OnNext("Event3");
            observer.OnCompleted();
            mappedDisposable.Dispose();
            disposed.ShouldBeTrue();
            //verify events were raised in order
            events.ShouldBe(new string[]
            {
                "OnNext: Event1",
                "OnNext: Error caught: InvalidTimeZoneException",
                "OnNext: Event3",
                "OnCompleted",
            });
        }
    }
}
