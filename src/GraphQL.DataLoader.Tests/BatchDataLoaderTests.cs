using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;
using Nito.AsyncEx;
using Shouldly;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class BatchDataLoaderTests : DataLoaderTestBase
    {
        public BatchDataLoaderTests()
        {
            Users.AddUsers(
                new User
                {
                    UserId = 1
                },
                new User
                {
                    UserId = 2
                }
            );
        }

        [Fact]
        public void Operations_Are_Batched()
        {
            User user1 = null;
            User user2 = null;

            // Run within an async context to make sure we won't deadlock
            AsyncContext.Run(async () =>
            {
                var loader = new BatchDataLoader<int, User>(Users.GetUsersByIdAsync);

                // Start async tasks to load by ID
                var task1 = loader.LoadAsync(1);
                var task2 = loader.LoadAsync(2);

                // Dispatch loading
                loader.Dispatch();

                // Now await tasks
                user1 = await task1;
                user2 = await task2;
            });

            user1.ShouldNotBeNull();
            user2.ShouldNotBeNull();

            user1.UserId.ShouldBe(1);
            user2.UserId.ShouldBe(2);

            // This should have been called only once to load in a single batch
            Users.GetUsersByIdCalledCount.ShouldBe(1, "Operations were not batched");
        }

        [Fact]
        public void Results_Are_Cached()
        {
            User user1 = null;
            User user2 = null;
            User user3 = null;

            // Run within an async context to make sure we won't deadlock
            AsyncContext.Run(async () =>
            {
                var loader = new BatchDataLoader<int, User>(Users.GetUsersByIdAsync);

                // Start async tasks to load by ID
                var task1 = loader.LoadAsync(1);
                var task2 = loader.LoadAsync(2);

                // Dispatch loading
                loader.Dispatch();

                // Now await tasks
                user1 = await task1;
                user2 = await task2;

                var task3 = loader.LoadAsync(1);

                task3.Status.ShouldBe(TaskStatus.RanToCompletion,
                    "Task should already be complete because value comes from cache");

                // This should not actually run the fetch delegate again
                loader.Dispatch();

                user3 = await task3;
            });

            user3.ShouldBeSameAs(user1);

            Users.GetUsersByIdCalledCount.ShouldBe(1);
        }

        [Fact]
        public void NonExistent_Key_Will_Return_Default_Value()
        {
            var nullObjectUser = new User();

            User user1 = null;
            User user2 = null;
            User user3 = null;

            // Run within an async context to make sure we won't deadlock
            AsyncContext.Run(async () =>
            {
                // There is no user with the ID of 3
                // 3 will not be in the returned Dictionary, so the DataLoader should use the specified default value
                var loader = new BatchDataLoader<int, User>(Users.GetUsersByIdAsync, defaultValue: nullObjectUser);

                // Start async tasks to load by ID
                var task1 = loader.LoadAsync(1);
                var task2 = loader.LoadAsync(2);
                var task3 = loader.LoadAsync(3);

                // Dispatch loading
                loader.Dispatch();

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
            var nullObjectUser = new User();

            User user1 = null;
            User user2 = null;
            User user3 = null;

            // There is no user with the ID of 3
            // 3 will not be in the returned Dictionary, so the DataLoader should use the specified default value
            var loader = new BatchDataLoader<int, User>(Users.GetUsersByIdAsync, defaultValue: nullObjectUser);

            // Start async tasks to load by ID
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);
            var task3 = loader.LoadAsync(3);

            // Dispatch loading
            loader.Dispatch();

            // Now await tasks
            user1 = await task1;
            user2 = await task2;
            user3 = await task3;

            // Load key 3 again. 
            var task3b = loader.LoadAsync(3);

            task3b.Status.ShouldBe(TaskStatus.RanToCompletion,
                "Should be cached because it was requested in the first batch even though it wasn't in the result dictionary");

            loader.Dispatch();

            var user3b = await task3b;

            user3.ShouldBeSameAs(nullObjectUser, "The DataLoader should use the supplied default value");

            Users.GetUsersByIdCalledCount.ShouldBe(1, "Results should have been cached from first batch");
        }

        [Fact]
        public async Task ToDictionary_Exception()
        {
            var loader = new BatchDataLoader<int, User>(Users.GetDuplicateUsersAsync, x => x.UserId);

            // Start async tasks to load by ID
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);

            // Dispatch loading
            loader.Dispatch();

            try
            {
                // Now await tasks
                var user1 = await task1;
                var user2 = await task2;
            }
            catch (ArgumentException ex) when (ex.Message == "An item with the same key has already been added. Key: 1")
            {
                // This is the exception we should get
            }
        }

        [Fact]
        public async Task Keys_Are_DeDuped()
        {
            var loader = new BatchDataLoader<int, User>(Users.GetUsersByIdAsync);

            // Start async tasks to load duplicate IDs
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(1);

            // Dispatch loading
            loader.Dispatch();

            // Now await tasks
            var user1 = await task1;
            var user1b = await task2;

            Users.GetUsersByIdCalledCount.ShouldBe(1);
            Users.GetUsersById_UserIds.Count().ShouldBe(1, "The keys passed to the fetch delegate should be de-duplicated");
        }
    }
}
