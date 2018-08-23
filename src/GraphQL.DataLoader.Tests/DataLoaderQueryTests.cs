using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class DataLoaderQueryTests : QueryTestBase
    {
        [Fact]
        public void SingleQueryRoot_Works()
        {
            var users = Fake.Users.Generate(2);

            var usersMock = Services.GetRequiredService<Mock<IUsersStore>>();

            usersMock.Setup(store => store.GetAllUsersAsync(default))
                .ReturnsAsync(users);

            AssertQuerySuccess<DataLoaderTestSchema>(
                query: "{ users { userId firstName } }",
                expected: @"
{ users: [
    {
        userId: 1,
        firstName: """ + users[0].FirstName + @"""
    },
    {
        userId: 2,
        firstName: """ + users[1].FirstName + @"""
    }
] }
",
                listenerType: typeof(DataLoaderDocumentListener)
            );

            usersMock.Verify(x => x.GetAllUsersAsync(default), Times.Once);
        }

        [Fact]
        public void TwoLevel_SingleResult_Works()
        {
            var users = Fake.Users.Generate(1);

            var order = Fake.Orders.Generate();
            order.UserId = users[0].UserId;

            var ordersMock = Services.GetRequiredService<Mock<IOrdersStore>>();
            var usersMock = Services.GetRequiredService<Mock<IUsersStore>>();

            ordersMock.Setup(x => x.GetOrderByIdAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new[] { order });

            usersMock.Setup(x => x.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(users.ToDictionary(x => x.UserId));

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
            firstName: """ + users[0].FirstName + @"""
        }
    }
}
",
                listenerType: typeof(DataLoaderDocumentListener)
            );

            ordersMock.Verify(x => x.GetOrderByIdAsync(new[] { 1 }), Times.Once);
            ordersMock.VerifyNoOtherCalls();

            usersMock.Verify(x => x.GetUsersByIdAsync(new[] { 1 }, default), Times.Once);
            usersMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TwoLevel_MultipleResults_OperationsAreBatched()
        {
            var users = Fake.Users.Generate(2);
            var orders = Fake.GenerateOrdersForUsers(users, 1);

            var ordersMock = Services.GetRequiredService<Mock<IOrdersStore>>();
            var usersMock = Services.GetRequiredService<Mock<IUsersStore>>();

            ordersMock.Setup(x => x.GetAllOrdersAsync())
                .ReturnsAsync(orders);

            usersMock.Setup(x => x.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
                .ReturnsAsync(users.ToDictionary(x => x.UserId));

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
            firstName: """ + users[0].FirstName + @"""
        }
    },
    {
        orderId: 2,
        user: {
            userId: 2,
            firstName: """ + users[1].FirstName + @"""
        }
    }]
}
",
                listenerType: typeof(DataLoaderDocumentListener)
            );

            ordersMock.Verify(x => x.GetAllOrdersAsync(), Times.Once);
            ordersMock.VerifyNoOtherCalls();

            usersMock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2 }, default), Times.Once);
            usersMock.VerifyNoOtherCalls();
        }
    }
}
