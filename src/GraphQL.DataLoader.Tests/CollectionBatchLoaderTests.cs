using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;
using Shouldly;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class CollectionBatchLoaderTests : DataLoaderTestBase
    {
        public CollectionBatchLoaderTests()
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

            Orders.AddOrders(
                new Order()
                {
                    OrderId = 1,
                    UserId = 1
                    
                },
                new Order()
                {
                    OrderId = 2,
                    UserId = 1
                }
            );
        }

        [Fact]
        public async Task NonExistent_Key_Should_Return_Empty()
        {
            var loader = new CollectionBatchDataLoader<int, Order>((ids, ct) => Orders.GetOrdersByUserIdAsync(ids));

            // Start async tasks to load by User ID
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);

            // Dispatch loading
            loader.Dispatch();

            var user1Orders = await task1;
            var user2Orders = await task2;

            user1Orders.ShouldNotBeNull();
            user2Orders.ShouldNotBeNull();

            user1Orders.Count().ShouldBe(2);
            user2Orders.Count().ShouldBe(0);

            // This should have been called only once to load in a single batch
            Orders.GetOrdersByUserIdCalledCount.ShouldBe(1, "Operations should be batched");
        }

        [Fact]
        public async Task All_Requested_Keys_Should_Be_Cached()
        {
            var loader = new CollectionBatchDataLoader<int, Order>((ids, ct) => Orders.GetOrdersByUserIdAsync(ids));

            // Start async tasks to load by User ID
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(2);

            // Dispatch loading
            loader.Dispatch();

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
            loader.Dispatch();

            var user1bOrders = await task1b;
            var user2bOrders = await task2b;
            var user3Orders = await task3;

            user1bOrders.ShouldNotBeNull();
            user2bOrders.ShouldNotBeNull();
            user3Orders.ShouldNotBeNull();

            user1Orders.Count().ShouldBe(2);
            user2Orders.Count().ShouldBe(0);
            user3Orders.Count().ShouldBe(0);


            // This should have been called only once to load in a single batch
            Orders.GetOrdersByUserIdCalledCount.ShouldBe(2, "Operations should be batched");
        }

        [Fact]
        public async Task Keys_Are_DeDuped()
        {
            var loader = new CollectionBatchDataLoader<int, Order>((ids, ct) => Orders.GetOrdersByUserIdAsync(ids));

            // Start async tasks to load duplicate keys
            var task1 = loader.LoadAsync(1);
            var task2 = loader.LoadAsync(1);

            // Dispatch loading
            loader.Dispatch();

            // Now await tasks
            var user1Orders = await task1;
            var user1bOrders = await task2;

            Orders.GetOrdersByUserIdCalledCount.ShouldBe(1);
            Orders.GetOrdersByUserId_UserIds.Count().ShouldBe(1, "The keys passed to the fetch delegate should be de-duplicated");
        }
    }
}
