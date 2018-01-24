using DataLoader.Tests.Models;
using DataLoader.Tests.Stores;
using GraphQL.DataLoader;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DataLoader.Tests
{
    public class DataLoaderTests : QueryTestBase
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
    }
}
