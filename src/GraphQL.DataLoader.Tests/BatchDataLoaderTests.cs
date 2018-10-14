using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using Moq;
using Nito.AsyncEx;
using Shouldly;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class BatchDataLoaderTests : DataLoaderTestBase
    {
        [Fact]
        public void Operations_Are_Batched()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => users.ToDictionary(x => x.UserId));

            var usersStore = mock.Object;

            User user1 = null;
            User user2 = null;

            // Run within an async context to make sure we won't deadlock
            AsyncContext.Run(async () =>
            {
                var loader = new BatchDataLoader<int, User>(_ => { }, usersStore.GetUsersByIdAsync);

                // Start async tasks to load by ID
                var task1 = loader.LoadAsync(1);
                var task2 = loader.LoadAsync(2);

                // Dispatch loading
                await await ((IDispatchableDataLoader)loader).DispatchAsync(CancellationToken.None);

                // Now await tasks
                user1 = await task1;
                user2 = await task2;
            });

            user1.ShouldNotBeNull();
            user2.ShouldNotBeNull();

            user1.UserId.ShouldBe(1);
            user2.UserId.ShouldBe(2);

            // This should have been called only once to load in a single batch
            mock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2 }, default), Times.Once);
        }

        [Fact]
        public void Results_Are_Cached()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => users.ToDictionary(x => x.UserId));

            var usersStore = mock.Object;

            User user1 = null;
            User user2 = null;
            User user3 = null;

            // Run within an async context to make sure we won't deadlock
            AsyncContext.Run(async () =>
            {
            var loader = new BatchDataLoader<int, User>(_ => { }, usersStore.GetUsersByIdAsync);

                // Start async tasks to load by ID
                var task1 = loader.LoadAsync(1);
                var task2 = loader.LoadAsync(2);

                // Dispatch loading
                await await ((IDispatchableDataLoader)loader).DispatchAsync(CancellationToken.None);

                // Now await tasks
                user1 = await task1;
                user2 = await task2;

                var task3 = loader.LoadAsync(1);

                task3.Status.ShouldBe(TaskStatus.RanToCompletion,
                    "Task should already be complete because value comes from cache");

                // This should not actually run the fetch delegate again
                await await ((IDispatchableDataLoader)loader).DispatchAsync(CancellationToken.None);

                user3 = await task3;
            });

            user3.ShouldBeSameAs(user1);

            mock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2 }, default), Times.Once);
        }

        [Fact]
        public void NonExistent_Key_Will_Return_Default_Value()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => users.ToDictionary(x => x.UserId));

            var usersStore = mock.Object;
            var nullObjectUser = new User();

            User user1 = null;
            User user2 = null;
            User user3 = null;

            // Run within an async context to make sure we won't deadlock
            AsyncContext.Run(async () =>
            {
                // There is no user with the ID of 3
                // 3 will not be in the returned Dictionary, so the DataLoader should use the specified default value
                var loader = new BatchDataLoader<int, User>(_ => { }, usersStore.GetUsersByIdAsync, defaultValue: nullObjectUser);

                // Start async tasks to load by ID
                var task1 = loader.LoadAsync(1);
                var task2 = loader.LoadAsync(2);
                var task3 = loader.LoadAsync(3);

                // Dispatch loading
                await await ((IDispatchableDataLoader)loader).DispatchAsync(CancellationToken.None);

                // Now await tasks
                user1 = await task1;
                user2 = await task2;
                user3 = await task3;
            });

            user1.ShouldNotBeNull();
            user2.ShouldNotBeNull();
            user3.ShouldNotBeNull();

            user1.UserId.ShouldBe(1);
            user2.UserId.ShouldBe(2);
            user3.ShouldBeSameAs(nullObjectUser, "The DataLoader should use the supplied default value");
        }

        [Fact]
        public async Task All_Requested_Keys_Should_Be_Cached()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => users.ToDictionary(x => x.UserId));

            var usersStore = mock.Object;

            var nullObjectUser = new User();

            User user1 = null;
            User user2 = null;
            User user3 = null;

            // There is no user with the ID of 3
            // 3 will not be in the returned Dictionary, so the DataLoader should use the specified default value
            var loader = new BatchDataLoader<int, User>(_ => { }, usersStore.GetUsersByIdAsync, defaultValue: nullObjectUser);

            // Start async tasks to load by ID
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);
            var task3 = loader.LoadAsync(3);

            // Dispatch loading
            await await ((IDispatchableDataLoader)loader).DispatchAsync(CancellationToken.None);

            // Now await tasks
            user1 = await task1;
            user2 = await task2;
            user3 = await task3;

            // Load key 3 again.
            var task3b = loader.LoadAsync(3);

            task3b.Status.ShouldBe(TaskStatus.RanToCompletion,
                "Should be cached because it was requested in the first batch even though it wasn't in the result dictionary");

            await await ((IDispatchableDataLoader)loader).DispatchAsync(CancellationToken.None);

            var user3b = await task3b;

            user3.ShouldBeSameAs(nullObjectUser, "The DataLoader should use the supplied default value");

            mock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2, 3 }, default), Times.Once,
                "Results should have been cached from first batch");
        }

        [Fact]
        public async Task ToDictionary_Exception()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            // Set duplicate user IDs
            users.ForEach(u => u.UserId = 1);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => users.ToDictionary(x => x.UserId));

            var usersStore = mock.Object;

            var loader = new BatchDataLoader<int, User>(_ => { }, usersStore.GetUsersByIdAsync);

            // Start async tasks to load by ID
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);

            // Dispatch loading
            await await ((IDispatchableDataLoader)loader).DispatchAsync(CancellationToken.None);

            Exception ex = await Should.ThrowAsync<ArgumentException>(async () =>
            {
                // Now await tasks
                var user1 = await task1;
                var user2 = await task2;
            });

            ex.Message.ShouldBe("An item with the same key has already been added. Key: 1");
        }

        [Fact]
        public async Task Keys_Are_DeDuped()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => users.ToDictionary(x => x.UserId));

            var usersStore = mock.Object;

            var loader = new BatchDataLoader<int, User>(_ => { }, usersStore.GetUsersByIdAsync);

            // Start async tasks to load duplicate IDs
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(1);

            // Dispatch loading
            await await ((IDispatchableDataLoader)loader).DispatchAsync(CancellationToken.None);

            // Now await tasks
            var user1 = await task1;
            var user1b = await task2;

            user1.ShouldBeSameAs(users[0]);
            user1b.ShouldBeSameAs(users[0]);

            mock.Verify(x => x.GetUsersByIdAsync(new[] { 1 }, default), Times.Once,
                "The keys passed to the fetch delegate should be de-duplicated");
        }
    }
}
