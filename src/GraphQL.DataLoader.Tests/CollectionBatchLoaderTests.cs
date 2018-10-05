using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using Moq;
using Shouldly;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
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
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);

            // Dispatch loading
            await loader.DispatchAsync();

            var user1Orders = await task1;
            var user2Orders = await task2;

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
                .ReturnsAsync(orders.ToLookup(o => o.UserId));

            var ordersStore = mock.Object;

            var loader = new CollectionBatchDataLoader<int, Order>(ordersStore.GetOrdersByUserIdAsync);

            // Start async tasks to load by User ID
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);

            // Dispatch loading
            await loader.DispatchAsync();

            var user1Orders = await task1;
            var user2Orders = await task2;

            user1Orders.ShouldNotBeNull();
            user2Orders.ShouldNotBeNull();

            user1Orders.Count().ShouldBe(2);
            user2Orders.Count().ShouldBe(0);

            // Request keys 1 and 2 again. Result should be cached.
            // Key 3 should NOT be cached

            var task1b = loader.LoadAsync(1);
            var task2b = loader.LoadAsync(2);
            var task3 = loader.LoadAsync(3);

            task1b.Status.ShouldBe(TaskStatus.RanToCompletion, "Result should already be cached");
            task2b.Status.ShouldBe(TaskStatus.RanToCompletion, "Result should already be cached");
            task3.Status.ShouldNotBe(TaskStatus.RanToCompletion, "Result should already be cached");

            // Dispatch loading
            await loader.DispatchAsync();

            var user1bOrders = await task1b;
            var user2bOrders = await task2b;
            var user3Orders = await task3;

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
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(1);

            // Dispatch loading
            await loader.DispatchAsync();

            // Now await tasks
            var user1Orders = await task1;
            var user1bOrders = await task2;

            mock.Verify(x => x.GetOrdersByUserIdAsync(new[] { 1 }, default), Times.Once,
                "The keys passed to the fetch delegate should be de-duplicated");
        }
    }
}
