using DataLoader.Tests.Models;
using DataLoader.Tests.Stores;
using GraphQL.Types;

namespace DataLoader.Tests.Types
{
    public class OrderType : ObjectGraphType<Order>
    {
        public OrderType(IDataLoaderContextAccessor accessor, UsersStore users)
        {
            Name = "Order";

            Field(x => x.OrderId);
            Field(x => x.OrderedOn);
            Field(x => x.Total);

            Field<UserType>()
                .Name("User")
                .Returns<User>()
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<int, User>("GetUsersById",
                        users.GetUsersByIdAsync);

                    return loader.LoadAsync(ctx.Source.UserId);
                });
        }
    }
}
