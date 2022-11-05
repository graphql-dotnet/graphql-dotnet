using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types;

public class OrderType : ObjectGraphType<Order>
{
    public OrderType(IDataLoaderContextAccessor accessor, IUsersStore users, IOrdersStore orders)
    {
        Name = "Order";

        Field(x => x.OrderId);
        Field(x => x.OrderedOn);

        Field<UserType, User>("User")
            .ResolveAsync(ctx =>
            {
                var loader = accessor.Context.GetOrAddBatchLoader<int, User>("GetUsersById",
                    users.GetUsersByIdAsync);

                return loader.LoadAsync(ctx.Source.UserId);
            });

        Field<ListGraphType<OrderItemType>, IEnumerable<OrderItem>>("Items")
            .ResolveAsync(ctx =>
            {
                var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, OrderItem>("GetOrderItemsById",
                    orders.GetItemsByOrderIdAsync);

                return loader.LoadAsync(ctx.Source.OrderId);
            });
    }
}
