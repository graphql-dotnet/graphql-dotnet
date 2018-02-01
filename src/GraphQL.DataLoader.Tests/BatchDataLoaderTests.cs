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
    }
}
