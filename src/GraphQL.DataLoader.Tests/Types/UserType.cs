using System.Collections.Generic;
using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType(IDataLoaderContextAccessor accessor, OrdersStore orders)
        {
            Name = "User";

            Field(x => x.UserId);
            Field(x => x.FirstName);
            Field(x => x.LastName);
            Field(x => x.Email);

            Field<ListGraphType<OrderType>, IEnumerable<Order>>()
                .Name("Orders")
                .ResolveAsync(ctx =>
                {
                    var ordersLoader = accessor.Context.GetOrAddCollectionBatchLoader<int, Order>("GetOrdersByUserId",
                        orders.GetOrdersByUserIdAsync);

                    return ordersLoader.LoadAsync(ctx.Source.UserId);
                });
        }
    }
}
