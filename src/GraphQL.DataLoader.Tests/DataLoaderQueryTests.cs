using GraphQL.DataLoader.Tests.Stores;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GraphQL.DataLoader.Tests;

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
{ ""users"": [
    {
        ""userId"": 1,
        ""firstName"": """ + users[0].FirstName + @"""
    },
    {
        ""userId"": 2,
        ""firstName"": """ + users[1].FirstName + @"""
    }
] }
");

        usersMock.Verify(x => x.GetAllUsersAsync(default), Times.Once);
    }

    [Fact]
    public void SingleQueryRootWithDelay_Works()
    {
        var users = Fake.Users.Generate(2);

        var usersMock = Services.GetRequiredService<Mock<IUsersStore>>();

        usersMock.Setup(store => store.GetAllUsersAsync(default))
            .ReturnsAsync(users);

        AssertQuerySuccess<DataLoaderTestSchema>(
            query: "{ usersWithDelay { userId firstName } }",
            expected: @"
{ ""usersWithDelay"": [
    {
        ""userId"": 1,
        ""firstName"": """ + users[0].FirstName + @"""
    },
    {
        ""userId"": 2,
        ""firstName"": """ + users[1].FirstName + @"""
    }
] }
");

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
    ""order"": {
        ""orderId"": 1,
        ""user"": {
            ""userId"": 1,
            ""firstName"": """ + users[0].FirstName + @"""
        }
    }
}
");

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
    ""orders"": [
    {
        ""orderId"": 1,
        ""user"": {
            ""userId"": 1,
            ""firstName"": """ + users[0].FirstName + @"""
        }
    },
    {
        ""orderId"": 2,
        ""user"": {
            ""userId"": 2,
            ""firstName"": """ + users[1].FirstName + @"""
        }
    }]
}
");

        ordersMock.Verify(x => x.GetAllOrdersAsync(), Times.Once);
        ordersMock.VerifyNoOtherCalls();

        usersMock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2 }, default), Times.Once);
        usersMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void TwoLevel_MultipleResults_OperationsAreBatched_SerialExecution()
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
mutation {
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
    ""orders"": [
    {
        ""orderId"": 1,
        ""user"": {
            ""userId"": 1,
            ""firstName"": """ + users[0].FirstName + @"""
        }
    },
    {
        ""orderId"": 2,
        ""user"": {
            ""userId"": 2,
            ""firstName"": """ + users[1].FirstName + @"""
        }
    }]
}
");

        ordersMock.Verify(x => x.GetAllOrdersAsync(), Times.Once);
        ordersMock.VerifyNoOtherCalls();

        usersMock.Verify(x => x.GetUsersByIdAsync(new[] { 1, 2 }, default), Times.Once);
        usersMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Chained_DataLoaders_Works()
    {
        var users = Fake.Users.Generate(2);
        var orders = Fake.GenerateOrdersForUsers(users, 2);
        var orderItems = Fake.GetItemsForOrders(orders, 2);

        var ordersMock = Services.GetRequiredService<Mock<IOrdersStore>>();
        var usersMock = Services.GetRequiredService<Mock<IUsersStore>>();

        usersMock.Setup(x => x.GetAllUsersAsync(default))
            .ReturnsAsync(users);

        ordersMock.Setup(store => store.GetOrdersByUserIdAsync(It.IsAny<IEnumerable<int>>(), default))
            .ReturnsAsync(orders.ToLookup(o => o.UserId));

        ordersMock.Setup(x => x.GetItemsByOrderIdAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(orderItems.ToLookup(x => x.OrderId));

        AssertQuerySuccess<DataLoaderTestSchema>(
            query: @"
{
    users {
        orderedItems {
            orderItemId
        }
    }
}",
            expected: @"
{
    ""users"": [
    {
        ""orderedItems"": [
        {
            ""orderItemId"": 1
        },
        {
            ""orderItemId"": 2
        },
        {
            ""orderItemId"": 3
        },
        {
            ""orderItemId"": 4
        }]
    },
    {
        ""orderedItems"": [
        {
            ""orderItemId"": 5
        },
        {
            ""orderItemId"": 6
        },
        {
            ""orderItemId"": 7
        },
        {
            ""orderItemId"": 8
        }]
    }]
}
");
        usersMock.Verify(x => x.GetAllUsersAsync(default), Times.Once);
        usersMock.VerifyNoOtherCalls();

        ordersMock.Verify(x => x.GetOrdersByUserIdAsync(new[] { 1, 2 }, default), Times.Once);
        ordersMock.Verify(x => x.GetItemsByOrderIdAsync(new[] { 1, 2, 3, 4 }), Times.Once);
        ordersMock.VerifyNoOtherCalls();

    }

    [Fact]
    public async Task Large_Query_Performance()
    {
        var users = Fake.Users.Generate(1000);
        var products = Fake.Products.Generate(2000);
        var orders = Fake.GenerateOrdersForUsers(users, 10);
        var orderItems = Fake.GetItemsForOrders(orders, 5);

        var ordersMock = Services.GetRequiredService<Mock<IOrdersStore>>();
        var usersMock = Services.GetRequiredService<Mock<IUsersStore>>();
        var productsMock = Services.GetRequiredService<Mock<IProductsStore>>();

        ordersMock.Setup(x => x.GetAllOrdersAsync())
            .ReturnsAsync(orders);

        ordersMock.Setup(x => x.GetItemsByOrderIdAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(orderItems.ToLookup(x => x.OrderId));

        usersMock.Setup(x => x.GetUsersByIdAsync(It.IsAny<IEnumerable<int>>(), default))
            .ReturnsAsync(users.ToDictionary(x => x.UserId));

        productsMock.Setup(x => x.GetProductsByIdAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(products.ToDictionary(x => x.ProductId));

        var result = await ExecuteQueryAsync<DataLoaderTestSchema>(
            query: @"
{
    orders {
        orderId
        orderedOn
        user {
            userId
            firstName
            lastName
            email
        }
        items {
            orderItemId
            quantity
            unitPrice
            product {
                productId
                name
                price
                description
            }
        }
    }
}").ConfigureAwait(false);

        result.Errors.ShouldBeNull();
    }

    [Fact]
    public void EnumerableDataLoaderResult_Works()
    {
        var users = Fake.Users.Generate(2);

        var usersMock = Services.GetRequiredService<Mock<IUsersStore>>();

        usersMock.Setup(store => store.GetUsersByIdAsync(new int[] { 1, 2, 3 }, default))
            .ReturnsAsync(users.ToDictionary(x => x.UserId));

        AssertQuerySuccess<DataLoaderTestSchema>(
            query: "{ specifiedUsers(ids:[1, 2, 3]) { userId firstName } }",
            expected: @"
{ ""specifiedUsers"": [
    {
        ""userId"": 1,
        ""firstName"": """ + users[0].FirstName + @"""
    },
    {
        ""userId"": 2,
        ""firstName"": """ + users[1].FirstName + @"""
    },
    null
] }
");

        usersMock.Verify(x => x.GetUsersByIdAsync(new int[] { 1, 2, 3 }, default), Times.Once);
    }

    [Fact]
    public void EnumerableDataLoaderResult_WithThen_Works()
    {
        var users = Fake.Users.Generate(2);

        var usersMock = Services.GetRequiredService<Mock<IUsersStore>>();

        usersMock.Setup(store => store.GetUsersByIdAsync(new int[] { 1, 2, 3 }, default))
            .ReturnsAsync(users.ToDictionary(x => x.UserId));

        AssertQuerySuccess<DataLoaderTestSchema>(
            query: "{ specifiedUsersWithThen(ids:[1, 2, 3]) { userId firstName } }",
            expected: @"
{ ""specifiedUsersWithThen"": [
    {
        ""userId"": 1,
        ""firstName"": """ + users[0].FirstName + @"""
    },
    {
        ""userId"": 2,
        ""firstName"": """ + users[1].FirstName + @"""
    }
] }
");

        usersMock.Verify(x => x.GetUsersByIdAsync(new int[] { 1, 2, 3 }, default), Times.Once);
    }

    /// <summary>
    /// Exercises Execution.ExecutionNode.ResolvedType for children of children, verifying that
    /// <see cref="Execution.ExecutionNode.GraphType"/> is returning proper values. Without a dataloader,
    /// Execution.ExecutionStrategy.SetArrayItemNodes(Execution.ExecutionContext, Execution.ArrayExecutionNode)
    /// skips execution of <see cref="Execution.ExecutionStrategy.ValidateNodeResult(Execution.ExecutionContext, Execution.ExecutionNode)"/>
    /// because it is not relevant, and that method is the only one that calls Execution.ExecutionNode.ResolvedType.
    /// </summary>
    [Fact]
    public void ExerciseListsOfLists()
    {
        AssertQuerySuccess<DataLoaderTestSchema>(
            query: "{ exerciseListsOfLists (values:[[1, 2], [3, 4, 5]]) }",
            expected: @"{ ""exerciseListsOfLists"": [[1, 2], [3, 4, 5]] }");
    }
}
