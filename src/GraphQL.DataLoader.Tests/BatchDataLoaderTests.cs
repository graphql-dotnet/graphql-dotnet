using System;
using System.Collections.Generic;
using System.Linq;
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
                .ReturnsAsync(users.ToDictionary(x => x.UserId), delay: TimeSpan.FromMilliseconds(20));

            var usersStore = mock.Object;

            User user1 = null;
            User user2 = null;

            // Run within an async context to make sure we won't deadlock
            AsyncContext.Run(async () =>
            {
                var loader = new BatchDataLoader<int, User>(usersStore.GetUsersByIdAsync);

                // Start async tasks to load by ID
                var result1 = loader.LoadAsync(1);
                var result2 = loader.LoadAsync(2);

                // Dispatch loading
                await loader.DispatchAsync();

                var task1 = result1.GetResultAsync();
                var task2 = result2.GetResultAsync();

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
        public void Batched_Honors_MaxBatchSize()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(4);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => users.ToDictionary(x => x.UserId));

            var usersStore = mock.Object;

            User user1 = null;
            User user2 = null;
            User user3 = null;
            User user4 = null;
            User user5 = null;

            // Run within an async context to make sure we won't deadlock
            AsyncContext.Run(async () =>
            {
                var loader = new BatchDataLoader<int, User>(usersStore.GetUsersByIdAsync, null, null, 2);

                // Start async tasks to load by ID
                var result1 = loader.LoadAsync(1);
                var result2 = loader.LoadAsync(2);
                var result3 = loader.LoadAsync(3);
                var result4 = loader.LoadAsync(4);
                var result5 = loader.LoadAsync(5);

                // Dispatch loading
                await loader.DispatchAsync();

                var task1 = result1.GetResultAsync();
                var task2 = result2.GetResultAsync();
                var task3 = result3.GetResultAsync();
                var task4 = result4.GetResultAsync();
                var task5 = result5.GetResultAsync();

                // Now await tasks
                user1 = await task1;
                user2 = await task2;
                user3 = await task3;
                user4 = await task4;
                user5 = await task5;
            });

            user1.ShouldNotBeNull();
            user2.ShouldNotBeNull();
            user3.ShouldNotBeNull();
            user4.ShouldNotBeNull();
            user5.ShouldBeNull();

            user1.UserId.ShouldBe(1);
            user2.UserId.ShouldBe(2);
            user3.UserId.ShouldBe(3);
            user4.UserId.ShouldBe(4);

            // This should have been called three times to load in three batch (maximum of 2 per batch)
            mock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2 }, default), Times.Once);
            mock.Verify(x => x.GetUsersByIdAsync(new[] { 3, 4 }, default), Times.Once);
            mock.Verify(x => x.GetUsersByIdAsync(new[] { 5 }, default), Times.Once);
            mock.VerifyNoOtherCalls();
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
                var loader = new BatchDataLoader<int, User>(usersStore.GetUsersByIdAsync);

                // Start async tasks to load by ID
                var result1 = loader.LoadAsync(1);
                var result2 = loader.LoadAsync(2);

                // Dispatch loading
                await loader.DispatchAsync();

                var task1 = result1.GetResultAsync();
                var task2 = result2.GetResultAsync();

                // Now await tasks
                user1 = await task1;
                user2 = await task2;

                var result3 = loader.LoadAsync(1);

                //testing status meaningless with new design
                var task3 = result3.GetResultAsync();
                task3.Status.ShouldBe(TaskStatus.RanToCompletion,
                    "Task should already be complete because value comes from cache");

                // This should not actually run the fetch delegate again
                await loader.DispatchAsync();

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
                var loader = new BatchDataLoader<int, User>(usersStore.GetUsersByIdAsync, defaultValue: nullObjectUser);

                // Start async tasks to load by ID
                var result1 = loader.LoadAsync(1);
                var result2 = loader.LoadAsync(2);
                var result3 = loader.LoadAsync(3);

                // Dispatch loading
                await loader.DispatchAsync();

                var task1 = result1.GetResultAsync();
                var task2 = result2.GetResultAsync();
                var task3 = result3.GetResultAsync();

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
            var loader = new BatchDataLoader<int, User>(usersStore.GetUsersByIdAsync, defaultValue: nullObjectUser);

            // Start async tasks to load by ID
            var result1 = loader.LoadAsync(1);
            var result2 = loader.LoadAsync(2);
            var result3 = loader.LoadAsync(3);

            // Dispatch loading
            await loader.DispatchAsync();

            var task1 = result1.GetResultAsync();
            var task2 = result2.GetResultAsync();
            var task3 = result3.GetResultAsync();

            // Now await tasks
            user1 = await task1;
            user2 = await task2;
            user3 = await task3;

            // Load key 3 again.
            var result3b = loader.LoadAsync(3);

            //testing status is meaningless with new design
            var task3b = result3b.GetResultAsync();
            task3b.Status.ShouldBe(TaskStatus.RanToCompletion,
                "Should be cached because it was requested in the first batch even though it wasn't in the result dictionary");

            await loader.DispatchAsync();

            var user3b = await task3b;

            user3.ShouldBeSameAs(nullObjectUser, "The DataLoader should use the supplied default value");

            mock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2, 3 }, default), Times.Once,
                "Results should have been cached from first batch");
            mock.VerifyNoOtherCalls();
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

            var loader = new BatchDataLoader<int, User>(usersStore.GetUsersByIdAsync);

            // Start async tasks to load by ID
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);

            // Dispatch loading
            //with new design, any errors would be thrown here; but this is not used by ExecutionStrategy anyway
            //await loader.DispatchAsync();

            Exception ex = await Should.ThrowAsync<ArgumentException>(async () =>
            {

                // Now await tasks
                var user1 = await task1.GetResultAsync();
                var user2 = await task2.GetResultAsync();
            });

            var actualException = Should.Throw<ArgumentException>(() =>
            {
                var d = new Dictionary<int, int>
                {
                    { 1, 1 },
                    { 1, 1 }
                };
            });

            ex.Message.ShouldBe(actualException.Message);
        }

        [Fact]
        public async Task Failing_DataLoaders_Only_Execute_Once()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            // Set duplicate user IDs
            users.ForEach(u => u.UserId = 1);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => throw new ApplicationException());

            var usersStore = mock.Object;

            var loader = new BatchDataLoader<int, User>(usersStore.GetUsersByIdAsync);

            // Start async tasks to load by ID
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);

            Exception ex = await Should.ThrowAsync<ApplicationException>(async () =>
            {
                // Now await tasks
                var user1 = await task1.GetResultAsync();
            });

            Exception ex2 = await Should.ThrowAsync<ApplicationException>(async () =>
            {
                // Now await tasks
                var user2 = await task2.GetResultAsync();
            });

            mock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2 }, default));
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Keys_Are_DeDuped()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => users.ToDictionary(x => x.UserId));

            var usersStore = mock.Object;

            var loader = new BatchDataLoader<int, User>(usersStore.GetUsersByIdAsync);

            // Start async tasks to load duplicate IDs
            var result1 = loader.LoadAsync(1);
            var result2 = loader.LoadAsync(1);

            // Dispatch loading
            await loader.DispatchAsync();

            var task1 = result1.GetResultAsync();
            var task2 = result2.GetResultAsync();

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
