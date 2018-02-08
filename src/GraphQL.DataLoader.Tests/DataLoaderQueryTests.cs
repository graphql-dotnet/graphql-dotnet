using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.DataLoader;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class DataLoaderQueryTests : QueryTestBase
    {
        [Fact]
        public void SingleQueryRoot_Works()
        {
            var users = Services.GetRequiredService<UsersStore>();

            users.AddUsers(new User
            {
                UserId = 1,
                FirstName = "John"
            },
            new User
            {
                UserId = 2,
                FirstName = "Bob"
            });

            AssertQuerySuccess<DataLoaderTestSchema>(
                query: "{ users { userId firstName } }",
                expected: @"
{ users: [
    {
        userId: 1,
        firstName: ""John""
    },
    {
        userId: 2,
        firstName: ""Bob""
    }
] }
",
                listenerType: typeof(DataLoaderDocumentListener)
            );

            users.GetAllUsersCalledCount.ShouldBe(1);
        }

        [Fact]
        public void TwoLevel_SingleResult_Works()
        {
            var orders = Services.GetRequiredService<OrdersStore>();
            var users = Services.GetRequiredService<UsersStore>();

            orders.AddOrders(new Order
            {
                OrderId = 1,
                UserId = 1,
                Total = 100.00m
            });

            users.AddUsers(new User
            {
                UserId = 1,
                FirstName = "John"
            });

            AssertQuerySuccess<DataLoaderTestSchema>(
                query: @"
{
    order(orderId: 1) {
        orderId
        user {
            userId
            firstName
        }
    }
}",
                expected: @"
{
    order: {
        orderId: 1,
        user: {
            userId: 1,
            firstName: ""John""
        }
    }
}
",
                listenerType: typeof(DataLoaderDocumentListener)
            );

            orders.GetOrderByIdCalledCount.ShouldBe(1);
            users.GetUsersByIdCalledCount.ShouldBe(1);
        }

        [Fact]
        public void TwoLevel_MultipleResults_OperationsAreBatched()
        {
            var orders = Services.GetRequiredService<OrdersStore>();
            var users = Services.GetRequiredService<UsersStore>();

            orders.AddOrders(new Order
            {
                OrderId = 1,
                UserId = 1,
                Total = 100.00m
            }, new Order
            {
                OrderId = 2,
                UserId = 2,
                Total = 50.00m
            });

            users.AddUsers(new User
            {
                UserId = 1,
                FirstName = "John"
            },
            new User
            {
                UserId = 2,
                FirstName = "Bob"
            });

            AssertQuerySuccess<DataLoaderTestSchema>(
                query: @"
{
    orders {
        orderId
        user {
            userId
            firstName
        }
    }
}",
                expected: @"
{
    orders: [
    {
        orderId: 1,
        user: {
            userId: 1,
            firstName: ""John""
        }
    },
    {
        orderId: 2,
        user: {
            userId: 2,
            firstName: ""Bob""
        }
    }]
}
",
                listenerType: typeof(DataLoaderDocumentListener)
            );

            orders.GetAllOrdersCalledCount.ShouldBe(1);
            users.GetUsersByIdCalledCount.ShouldBe(1, "Second level resolution not batched");
        }
    }
}
