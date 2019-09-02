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
            var task3 = loader.LoadAsync(new[] { 1, 2 });

            // Dispatch loading
            await loader.DispatchAsync();

            IEnumerable<Order> user1Orders;
            IEnumerable<Order> user2Orders;
            void assert()
            {
                user1Orders.ShouldNotBeNull();
                user2Orders.ShouldNotBeNull();

                user1Orders.Count().ShouldBe(2);
                user2Orders.Count().ShouldBe(0);
            };

            user1Orders = await task1;
            user2Orders = await task2;
            assert();

            user1Orders = (await task3)[0];
            user2Orders = (await task3)[1];
            assert();

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
            var task3 = loader.LoadAsync(new int[] { 3, 4 });

            // Dispatch loading
            await loader.DispatchAsync();

            var user1Orders = await task1;
            var user2Orders = await task2;
            var user3Orders = (await task3)[0];
            var user4Orders = (await task3)[0];

            user1Orders.ShouldNotBeNull();
            user2Orders.ShouldNotBeNull();
            user3Orders.ShouldNotBeNull();
            user4Orders.ShouldNotBeNull();

            user1Orders.Count().ShouldBe(2);
            user2Orders.Count().ShouldBe(0);
            user3Orders.Count().ShouldBe(0);
            user4Orders.Count().ShouldBe(0);

            // Request keys 1 and 2 again. And 3 and 4. Result should be cached.
            // Key 5, 6 and 7 should NOT be cached

            var task1b = loader.LoadAsync(1);
            var task2b = loader.LoadAsync(2);
            var task4 = loader.LoadAsync(5);
            var task5 = loader.LoadAsync(new int[] { 1, 2, 3, 4 });
            var task6 = loader.LoadAsync(new int[] { 6, 7 });

            task1b.Status.ShouldBe(TaskStatus.RanToCompletion, "Result should already be cached");
            task2b.Status.ShouldBe(TaskStatus.RanToCompletion, "Result should already be cached");
            task3.Status.ShouldBe(TaskStatus.RanToCompletion, "Result should already be cached");
            task4.Status.ShouldNotBe(TaskStatus.RanToCompletion, "Result should not already be cached");
            task5.Status.ShouldBe(TaskStatus.RanToCompletion, "Result should already be cached");
            task6.Status.ShouldNotBe(TaskStatus.RanToCompletion, "Result should not already be cached");

            // Dispatch loading
            await loader.DispatchAsync();

            var user1bOrders = await task1b;
            var user2bOrders = await task2b;
            var user3bOrders = (await task5)[2];
            var user4bOrders = (await task5)[3];
            var user5Orders = await task4;

            user1bOrders.ShouldNotBeNull();
            user2bOrders.ShouldNotBeNull();
            user3bOrders.ShouldNotBeNull();
            user4bOrders.ShouldNotBeNull();
            user5Orders.ShouldNotBeNull();

            user1bOrders.Count().ShouldBe(2);
            user2bOrders.Count().ShouldBe(0);
            user3bOrders.Count().ShouldBe(0);
            user4bOrders.Count().ShouldBe(0);
            user5Orders.Count().ShouldBe(0);

            // Verify calls to order store were cached properly
            mock.Verify(x => x.GetOrdersByUserIdAsync(new[] { 1, 2, 3, 4 }, default), Times.Once);
            mock.Verify(x => x.GetOrdersByUserIdAsync(new[] { 5, 6, 7 }, default), Times.Once);
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
            var task3 = loader.LoadAsync(new int[] { 1, 1 });

            // Dispatch loading
            await loader.DispatchAsync();

            // Now await tasks
            var user1Orders = await task1;
            var user1bOrders = await task2;
            var user1c1Orders = (await task3)[0];
            var user1c2Orders = (await task3)[1];

            mock.Verify(x => x.GetOrdersByUserIdAsync(new[] { 1 }, default), Times.Once,
                "The keys passed to the fetch delegate should be de-duplicated");
        }
    }
}
