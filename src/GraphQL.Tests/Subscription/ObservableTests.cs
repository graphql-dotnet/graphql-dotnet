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

        [Fact]
        public async Task SelectAsync_DataArrivesInOrder()
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
            var finishedEvents = new List<string>();
            var mappedObservable = observableMock.Object.SelectAsync(async (value, token) =>
            {
                await Task.Delay(int.Parse(value));
                lock (finishedEvents)
                    finishedEvents.Add(value);
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
            //these next events are all triggered immediately, but may cause a delay during SelectAsync
            observer.OnNext("1000"); // 1-second delay
            observer.OnError(new InvalidTimeZoneException()); //error is thrown
            observer.OnNext("500"); // 0.5-second delay
            observer.OnCompleted(); // completion
            await Task.Delay(2000);
            mappedDisposable.Dispose();
            disposed.ShouldBeTrue();
            //verify events were raised in order
            events.ShouldBe(new string[]
            {
                "OnNext: 1000",
                "OnError: InvalidTimeZoneException",
                "OnNext: 500",
                "OnCompleted",
            });
            //verify that async operations were completed out of order
            finishedEvents.ShouldBe(new string[]
            {
                "500",
                "1000",
            });
        }

        [Fact]
        public async Task SelectAsync_MultipleSequences()
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
            var finishedEvents = new List<string>();
            var mappedObservable = observableMock.Object.SelectAsync(async (value, token) =>
            {
                await Task.Delay(int.Parse(value));
                lock (finishedEvents)
                    finishedEvents.Add(value);
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
            observer.OnNext("500");
            observer.OnNext("50");
            await Task.Delay(1000);
            observer.OnNext("500");
            observer.OnNext("50");
            await Task.Delay(1000);
            mappedDisposable.Dispose();
            disposed.ShouldBeTrue();
            //verify events were raised in order
            events.ShouldBe(new string[]
            {
                "OnNext: 500",
                "OnNext: 50",
                "OnNext: 500",
                "OnNext: 50",
            });
            //verify that async operations were completed out of order
            finishedEvents.ShouldBe(new string[]
            {
                "50",
                "500",
                "50",
                "500",
            });
        }

        [Fact]
        public async Task SelectAsync_NoEventsProducedAfterDispose()
        {
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
                await Task.Delay(int.Parse(value));
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
            observer.OnNext("50");
            observer.OnNext("1500");
            observer.OnError(new InvalidTimeZoneException());
            observer.OnNext("60");
            observer.OnCompleted();
            await Task.Delay(500);
            mappedDisposable.Dispose();
            disposed.ShouldBeTrue();
            await Task.Delay(2500);
            events.ShouldBe(new string[]
            {
                "OnNext: 50",
            });
            canceledEvents.ShouldBe(new string[]
            {
                "1500",
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
