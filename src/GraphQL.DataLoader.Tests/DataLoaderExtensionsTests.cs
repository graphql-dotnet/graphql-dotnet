using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using Moq;

namespace GraphQL.DataLoader.Tests;

public class DataLoaderExtensionsTests : DataLoaderTestBase
{
    private Mock<IDataLoader<int, User>> GetMockDataLoader()
    {
        var mock = new Mock<IDataLoader<int, User>>();
        mock.Setup(dl => dl.LoadAsync(It.IsAny<int>()))
            .Returns((int key) => new DataLoaderResult<User>(new User() { UserId = key }));
        return mock;
    }

    [Fact]
    public async Task LoadAsync_MultipleKeys_CallsSingularMultipleTimes()
    {
        var mock = GetMockDataLoader();
        var keys = new[] { 1, 2 };
        var users = await DataLoaderExtensions.LoadAsync(mock.Object, keys).GetResultAsync().ConfigureAwait(false);

        users.ShouldNotBeNull();
        users.Length.ShouldBe(keys.Length, "Should return array of same length as number of keys provided");

        users[0].ShouldNotBeNull();
        users[1].ShouldNotBeNull();

        users[0].UserId.ShouldBe(keys[0]);
        users[1].UserId.ShouldBe(keys[1]);

        mock.Verify(x => x.LoadAsync(It.IsAny<int>()), Times.Exactly(keys.Length));
    }

    [Fact]
    public async Task LoadAsync_MultipleKeysAsParams_CallsSingularMultipleTimes()
    {
        var mock = GetMockDataLoader();
        var users = await DataLoaderExtensions.LoadAsync(mock.Object, 1, 2).GetResultAsync().ConfigureAwait(false);

        users.ShouldNotBeNull();
        users.Length.ShouldBe(2, "Should return array of same length as number of keys provided");

        users[0].ShouldNotBeNull();
        users[1].ShouldNotBeNull();

        users[0].UserId.ShouldBe(1);
        users[1].UserId.ShouldBe(2);

        mock.Verify(x => x.LoadAsync(It.IsAny<int>()), Times.Exactly(2));
    }

    [Fact]
    public async Task LoadAsync_MultipleKeys_Works()
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

        var loader = new BatchDataLoader<int, User>(usersStore.GetUsersByIdAsync);

        // Start async tasks to load by ID
        var result1 = loader.LoadAsync(new int[] { 1, 2 });
        var result2 = loader.LoadAsync(new int[] { 1, 2, 3 });

        // Dispatch loading
        await loader.DispatchAsync().ConfigureAwait(false);
        var task1 = result1.GetResultAsync();
        var task2 = result2.GetResultAsync();

        // Now await tasks
        users1 = await task1.ConfigureAwait(false);
        users1.ShouldNotBeNull();
        user1 = users1[0];
        user2 = users1[1];

        users2 = await task2.ConfigureAwait(false);
        users2.ShouldNotBeNull();
        user3 = users2[2];

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
