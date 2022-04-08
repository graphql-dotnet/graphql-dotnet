using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using Moq;

namespace GraphQL.DataLoader.Tests;

public class CollectionBatchLoaderTests : DataLoaderTestBase
{
    [Fact]
    public async Task NonExistent_Key_Should_Return_Empty()
    {
        var mock = new Mock<IOrdersStore>();

        var orders = Fake.Orders.Generate(2);
        orders.ForEach(o => o.UserId = 1);

        mock.Setup(store => store.GetOrdersByUserIdAsync(It.IsAny<IEnumerable<int>>(), default))
            .ReturnsAsync(orders.ToLookup(o => o.UserId));

        var ordersStore = mock.Object;

        var loader = new CollectionBatchDataLoader<int, Order>(ordersStore.GetOrdersByUserIdAsync);

        // Start async tasks to load by User ID
        var result1 = loader.LoadAsync(1);
        var result2 = loader.LoadAsync(2);

        // Dispatch loading
        await loader.DispatchAsync().ConfigureAwait(false);

        var task1 = result1.GetResultAsync();
        var task2 = result2.GetResultAsync();

        var user1Orders = await task1.ConfigureAwait(false);
        var user2Orders = await task2.ConfigureAwait(false);

        user1Orders.ShouldNotBeNull();
        user2Orders.ShouldNotBeNull();

        user1Orders.Count().ShouldBe(2);
        user2Orders.Count().ShouldBe(0);

        // This should have been called only once to load in a single batch
        mock.Verify(x => x.GetOrdersByUserIdAsync(new[] { 1, 2 }, default), Times.Once,
            "Operations should be batched");

        mock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task All_Requested_Keys_Should_Be_Cached()
    {
        var mock = new Mock<IOrdersStore>();

        var orders = Fake.Orders.Generate(2);
        orders.ForEach(o => o.UserId = 1);

        mock.Setup(store => store.GetOrdersByUserIdAsync(It.IsAny<IEnumerable<int>>(), default))
            .ReturnsAsync(orders.ToLookup(o => o.UserId), delay: TimeSpan.FromMilliseconds(20));

        var ordersStore = mock.Object;

        var loader = new CollectionBatchDataLoader<int, Order>(ordersStore.GetOrdersByUserIdAsync);

        // Start async tasks to load by User ID
        var result1 = loader.LoadAsync(1);
        var result2 = loader.LoadAsync(2);

        // Dispatch loading
        await loader.DispatchAsync().ConfigureAwait(false);

        var task1 = result1.GetResultAsync();
        var task2 = result2.GetResultAsync();

        var user1Orders = await task1.ConfigureAwait(false);
        var user2Orders = await task2.ConfigureAwait(false);

        user1Orders.ShouldNotBeNull();
        user2Orders.ShouldNotBeNull();

        user1Orders.Count().ShouldBe(2);
        user2Orders.Count().ShouldBe(0);

        // Request keys 1 and 2 again. Result should be cached.
        // Key 3 should NOT be cached

        //due to the new dataloader design, these status checks are meaningless
        var task1b = loader.LoadAsync(1).GetResultAsync();
        var task2b = loader.LoadAsync(2).GetResultAsync();
        var task3 = loader.LoadAsync(3).GetResultAsync();

        task1b.Status.ShouldBe(TaskStatus.RanToCompletion, "Result should already be cached");
        task2b.Status.ShouldBe(TaskStatus.RanToCompletion, "Result should already be cached");
        //task3.Status.ShouldNotBe(TaskStatus.RanToCompletion, "Result should already be cached");

        // Dispatch loading
        await loader.DispatchAsync().ConfigureAwait(false);

        var user1bOrders = await task1b.ConfigureAwait(false);
        var user2bOrders = await task2b.ConfigureAwait(false);
        var user3Orders = await task3.ConfigureAwait(false);

        user1bOrders.ShouldNotBeNull();
        user2bOrders.ShouldNotBeNull();
        user3Orders.ShouldNotBeNull();

        user1Orders.Count().ShouldBe(2);
        user2Orders.Count().ShouldBe(0);
        user3Orders.Count().ShouldBe(0);

        // Verify calls to order store were cached properly
        mock.Verify(x => x.GetOrdersByUserIdAsync(new[] { 1, 2 }, default), Times.Once);
        mock.Verify(x => x.GetOrdersByUserIdAsync(new[] { 3 }, default), Times.Once);
        mock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Keys_Are_DeDuped()
    {
        var mock = new Mock<IOrdersStore>();

        var orders = Fake.Orders.Generate(2);
        orders.ForEach(o => o.UserId = 1);

        mock.Setup(store => store.GetOrdersByUserIdAsync(It.IsAny<IEnumerable<int>>(), default))
            .ReturnsAsync(orders.ToLookup(o => o.UserId));

        var ordersStore = mock.Object;

        var loader = new CollectionBatchDataLoader<int, Order>(ordersStore.GetOrdersByUserIdAsync);

        // Start async tasks to load duplicate keys
        var result1 = loader.LoadAsync(1);
        var result2 = loader.LoadAsync(1);

        // Dispatch loading
        await loader.DispatchAsync().ConfigureAwait(false);

        var task1 = result1.GetResultAsync();
        var task2 = result2.GetResultAsync();

        // Now await tasks
        var user1Orders = await task1.ConfigureAwait(false);
        var user1bOrders = await task2.ConfigureAwait(false);

        mock.Verify(x => x.GetOrdersByUserIdAsync(new[] { 1 }, default), Times.Once,
            "The keys passed to the fetch delegate should be de-duplicated");
    }

    [Fact]
    public async Task Returns_Null_For_Null_Reference_Types()
    {
        var loader = new CollectionBatchDataLoader<object, string>((_, _) => throw new Exception());
        (await loader.LoadAsync(null).GetResultAsync().ConfigureAwait(false)).ShouldBeNull();
    }

    [Fact]
    public async Task Returns_Null_For_Null_Value_Types()
    {
        var loader = new CollectionBatchDataLoader<int?, string>((_, _) => throw new Exception());
        (await loader.LoadAsync(null).GetResultAsync().ConfigureAwait(false)).ShouldBeNull();
    }
}
