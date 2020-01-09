using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class SimpleDataLoaderTests : DataLoaderTestBase
    {
        [Fact]
        public async Task Result_Is_Cached()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetAllUsersAsync(default))
                .ReturnsAsync(users, delay: TimeSpan.FromMilliseconds(20));

            var usersStore = mock.Object;

            var loader = new SimpleDataLoader<IEnumerable<User>>(usersStore.GetAllUsersAsync);

            var task = loader.LoadAsync();

            await loader.DispatchAsync();

            var result1 = await task;

            result1.ShouldNotBeNull();
            result1.Count().ShouldBe(2);

            // Load again. Result should be cached
            var task2 = loader.LoadAsync();

            task2.Status.ShouldBe(TaskStatus.RanToCompletion);

            var result2 = await task2;

            // Results should be the same instance
            result2.ShouldBeSameAs(result1);

            mock.Verify(x => x.GetAllUsersAsync(default), Times.Once);
        }

        [Fact]
        public async Task Operation_Can_Be_Cancelled()
        {
            var cts = new CancellationTokenSource();

            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetAllUsersAsync(cts.Token))
                .Returns(async (CancellationToken ct) =>
                {
                    await Task.Delay(100);
                    ct.ThrowIfCancellationRequested();

                    return users;
                });

            var usersStore = mock.Object;

            var loader = new SimpleDataLoader<IEnumerable<User>>(usersStore.GetAllUsersAsync);

            var task = loader.LoadAsync();

            cts.CancelAfter(TimeSpan.FromMilliseconds(5));

            await loader.DispatchAsync(cts.Token);

            await Should.ThrowAsync<TaskCanceledException>(task);

            mock.Verify(x => x.GetAllUsersAsync(cts.Token), Times.Once);
        }

        [Fact]
        public async Task Operation_Cancelled_Before_Dispatch_Does_Not_Execute()
        {
            var cts = new CancellationTokenSource();
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetAllUsersAsync(cts.Token))
                .ReturnsAsync(users, delay: TimeSpan.FromMilliseconds(20));

            var usersStore = mock.Object;

            var loader = new SimpleDataLoader<IEnumerable<User>>(usersStore.GetAllUsersAsync);

            var task = loader.LoadAsync();

            cts.Cancel();

            await loader.DispatchAsync(cts.Token);

            await Should.ThrowAsync<TaskCanceledException>(task);

            // Fetch delegate should not be called
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Deferred_Exception_Is_Bubbled_Properly()
        {
            var mock = new Mock<IUsersStore>();

            mock.Setup(store => store.GetAllUsersAsync(default))
                .Returns(async () =>
                {
                    await Task.Yield();
                    throw new Exception("Deferred");
                });

            var usersStore = mock.Object;

            var loader = new SimpleDataLoader<IEnumerable<User>>(usersStore.GetAllUsersAsync);

            var task = loader.LoadAsync();

            await loader.DispatchAsync();

            var ex = await Should.ThrowAsync<Exception>(task);

            ex.Message.ShouldBe("Deferred");
        }

        [Fact]
        public async Task Immediate_Exception_Is_Bubbled_Properly()
        {
            var mock = new Mock<IUsersStore>();

            mock.Setup(store => store.GetAllUsersAsync(default))
                .Returns(() => throw new Exception("Immediate"));

            var usersStore = mock.Object;

            var loader = new SimpleDataLoader<IEnumerable<User>>(usersStore.GetAllUsersAsync);

            var task = loader.LoadAsync();
            await loader.DispatchAsync();

            var ex = await Should.ThrowAsync<Exception>(task);

            ex.Message.ShouldBe("Immediate");
        }
    }
}
