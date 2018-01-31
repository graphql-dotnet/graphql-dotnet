using System.Collections.Generic;
using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types
{
    public class QueryType : ObjectGraphType
    {
        public QueryType(IDataLoaderContextAccessor accessor, UsersStore users, OrdersStore orders)
        {
            Name = "Query";

            Field<ListGraphType<UserType>, IEnumerable<User>>()
                .Name("Users")
                .Description("Get all Users")
                .Returns<IEnumerable<User>>()
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddLoader("GetAllUsers",
                        () => users.GetAllUsersAsync());

                    return loader.LoadAsync();
                });

            Field<OrderType, Order>()
                .Name("Order")
                .Description("Get Order by ID")
                .Argument<NonNullGraphType<IntGraphType>>("orderId", "")
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<int, Order>("GetOrderById",
                        orders.GetOrderByIdAsync, x => x.OrderId);

                    return loader.LoadAsync(ctx.GetArgument<int>("orderId"));
                });

            Field<ListGraphType<OrderType>, IEnumerable<Order>>()
                .Name("Orders")
                .Description("Get all Orders")
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddLoader("GetAllOrders",
                        orders.GetAllOrdersAsync);

                    return loader.LoadAsync();
                });
        }
    }
}
