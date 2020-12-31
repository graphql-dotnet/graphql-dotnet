using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Types;
using Moq;
using Shouldly;
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

            var delayResult = loader.LoadAsync();

            await loader.DispatchAsync();

            var result1 = await delayResult.GetResultAsync();

            result1.ShouldNotBeNull();
            result1.Count().ShouldBe(2);

            // Load again. Result should be cached
            var delayResult2 = loader.LoadAsync();
            var task2 = delayResult2.GetResultAsync();

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
                    await Task.Delay(60000, ct);
                    ct.ThrowIfCancellationRequested();

                    return users;
                });

            var usersStore = mock.Object;

            var loader = new SimpleDataLoader<IEnumerable<User>>(usersStore.GetAllUsersAsync);

            var result = loader.LoadAsync();

            cts.CancelAfter(TimeSpan.FromMilliseconds(5));

            var task = result.GetResultAsync(cts.Token);

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

            var result = loader.LoadAsync();

            cts.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(() => result.GetResultAsync(cts.Token));

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

            var result = loader.LoadAsync();

            var task = result.GetResultAsync();

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

            var result = loader.LoadAsync();

            var ex = await Should.ThrowAsync<Exception>(() => result.GetResultAsync());

            ex.Message.ShouldBe("Immediate");
        }

        [Fact]
        public void GetGraphTypeFromType_Works_With_IDataLoaderResult()
        {
            var type = typeof(IDataLoaderResult<int>);
            var result = type.GetGraphTypeFromType(false);
            result.ShouldBe(typeof(NonNullGraphType<IntGraphType>));
        }
    }
}
