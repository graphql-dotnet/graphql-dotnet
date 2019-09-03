using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using Moq;
using Nito.AsyncEx;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class DataLoaderExtensionsTests : DataLoaderTestBase
    {
        [Fact]
        public void LoadAsync_MultipleKeys_Works()
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
                var task1 = loader.LoadAsync(new int[] { 1, 2 });
                var task2 = loader.LoadAsync(new int[] { 1, 2, 3 });

                // Dispatch loading
                await loader.DispatchAsync();

                // Now await tasks
                user1 = (await task1)[0];
                user2 = (await task1)[1];
                user3 = (await task2)[2];
            });

            user1.ShouldNotBeNull();
            user2.ShouldNotBeNull();
            user3.ShouldBeNull();

            user1.UserId.ShouldBe(1);
            user2.UserId.ShouldBe(2);

            // This should have been called only once to load in a single batch
            mock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2, 3 }, default), Times.Once);
        }
    }
}
