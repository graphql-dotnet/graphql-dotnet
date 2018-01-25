using System.Collections.Generic;
using DataLoader.Tests.Models;
using DataLoader.Tests.Stores;
using GraphQL.Types;

namespace DataLoader.Tests.Types
{
    public class QueryType : ObjectGraphType
    {
        public QueryType(IDataLoaderContextAccessor accessor, UsersStore users, OrdersStore orders)
        {
            Name = "Query";

            Field<ListGraphType<UserType>>()
                .Name("Users")
                .Description("Get all Users")
                .Returns<IEnumerable<User>>()
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddLoader("GetAllUsers",
                        users.GetAllUsersAsync);

                    return loader.LoadAsync();
                });

            Field<OrderType>()
                .Name("Order")
                .Description("Get Order by ID")
                .Argument<NonNullGraphType<IntGraphType>>("orderId", "")
                .Returns<Order>()
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<int, Order>("GetOrderById",
                        orders.GetOrderByIdAsync, x => x.OrderId);

                    return loader.LoadAsync(ctx.GetArgument<int>("orderId"));
                });

            Field<ListGraphType<OrderType>>()
                .Name("Orders")
                .Description("Get all Orders")
                .Returns<IEnumerable<Order>>()
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddLoader("GetAllOrders",
                        orders.GetAllOrdersAsync);

                    return loader.LoadAsync();
                });
        }
    }
}
