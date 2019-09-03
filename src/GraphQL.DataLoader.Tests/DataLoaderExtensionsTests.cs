using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using Moq;
using Nito.AsyncEx;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class DataLoaderExtensionsTests : DataLoaderTestBase
    {
        [Fact]
        public async Task LoadAsync_MultipleKeys_CallsSingularMultipleTimes()
        {
            var mock = new Mock<IDataLoader<int, User>>();
            mock.Setup(dl => dl.LoadAsync(It.IsAny<int>()))
                .ReturnsAsync((int key) => new User() { UserId = key });

            var keys = new[] { 1, 2 };
            var users = await DataLoaderExtensions.LoadAsync(mock.Object, keys);

            users.ShouldNotBeNull();
            users.Length.ShouldBe(keys.Length, "Should return array of same length as number of keys provided");

            users[0].ShouldNotBeNull();
            users[1].ShouldNotBeNull();

            users[0].UserId.ShouldBe(keys[0]);
            users[1].UserId.ShouldBe(keys[1]);

            mock.Verify(x => x.LoadAsync(It.IsAny<int>()), Times.Exactly(keys.Length));
        }

        [Fact]
        public void LoadAsync_MultipleKeys_Works()
        {
            var mock = new Mock<IUsersStore>();
            var users = Fake.Users.Generate(2);

            mock.Setup(store => store.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(() => users.ToDictionary(x => x.UserId));

            var usersStore = mock.Object;

            User[] users1 = null;
            User[] users2 = null;
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
                users1 = (await task1);
                users1.ShouldNotBeNull();
                user1 = users1[0];
                user2 = users1[1];

                users2 = (await task2);
                users2.ShouldNotBeNull();
                user3 = users2[2];
            });

            users1.Length.ShouldBe(2, "First task with two keys should return array of two users");
            users2.Length.ShouldBe(3, "Second task with three keys should return array of three users");

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
