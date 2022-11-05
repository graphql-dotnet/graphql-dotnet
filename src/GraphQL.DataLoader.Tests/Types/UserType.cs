using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types;

public class UserType : ObjectGraphType<User>
{
    public UserType(IDataLoaderContextAccessor accessor, IOrdersStore orders)
    {
        Name = "User";

        Field(x => x.UserId);
        Field(x => x.FirstName);
        Field(x => x.LastName);
        Field(x => x.Email);

        Field<ListGraphType<OrderType>, IEnumerable<Order>>("Orders")
            .ResolveAsync(ctx =>
            {
                var ordersLoader = accessor.Context.GetOrAddCollectionBatchLoader<int, Order>("GetOrdersByUserId",
                    orders.GetOrdersByUserIdAsync);

                return ordersLoader.LoadAsync(ctx.Source.UserId);
            });

        Field<ListGraphType<OrderItemType>, IEnumerable<OrderItem>>("OrderedItems")
            .ResolveAsync(ctx =>
            {
                //obtain a reference to the GetOrdersByUserId batch loader
                var ordersLoader = accessor.Context.GetOrAddCollectionBatchLoader<int, Order>("GetOrdersByUserId",
                    orders.GetOrdersByUserIdAsync);

                //wait for dataloader to pull the orders for this user
                var ret = ordersLoader.LoadAsync(ctx.Source.UserId).Then(orderResults =>
                {
                    //obtain a reference to the GetOrderItemsById batch loader
                    var itemsLoader = accessor.Context.GetOrAddCollectionBatchLoader<int, OrderItem>("GetOrderItemsById",
                        orders.GetItemsByOrderIdAsync);

                    //wait for dataloader to pull the items for each order
                    //without dataloader, this would be:
                    //var batchResults = await orders.GetItemsByOrderIdAsync(orderResults.Select(o => o.OrderId));
                    //var allResults = orderResults.Select(o => batchResults[o.OrderId]);
                    return itemsLoader.LoadAsync(orderResults.Select(o => o.OrderId))
                        .Then(allResults => allResults.SelectMany(x => x)); //flatten and return the results

                });

                return ret;
            });
    }
}
