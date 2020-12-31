using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types
{
    public class QueryType : ObjectGraphType
    {
        public QueryType(IDataLoaderContextAccessor accessor, IUsersStore users, IOrdersStore orders)
        {
            Name = "Query";

            Field<ListGraphType<UserType>, IEnumerable<User>>()
                .Name("Users")
                .Description("Get all Users")
                .Returns<IEnumerable<User>>()
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddLoader("GetAllUsers",
                        users.GetAllUsersAsync);

                    return loader.LoadAsync();
                });

            Field<ListGraphType<UserType>, IEnumerable<User>>()
                .Name("UsersWithDelay")
                .Description("Get all Users")
                .Returns<IEnumerable<User>>()
                .ResolveAsync(async ctx =>
                {
                    await System.Threading.Tasks.Task.Delay(20);

                    var loader = accessor.Context.GetOrAddLoader("GetAllUsersWithDelay",
                        users.GetAllUsersAsync);

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

            Field<NonNullGraphType<ListGraphType<UserType>>, IEnumerable<IDataLoaderResult<User>>>()
                .Name("SpecifiedUsers")
                .Description("Get Users by ID")
                .Argument<NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>>("ids")
                .Resolve(ctx =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<int, User>("GetUserById",
                        users.GetUsersByIdAsync);

                    var ids = ctx.GetArgument<IEnumerable<int>>("ids");
                    var ret = ids.Select(id => loader.LoadAsync(id));
                    return ret;
                });

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>>>>()
                .Name("ExerciseListsOfLists")
                .Argument<ListGraphType<ListGraphType<IntGraphType>>>("values")
                .Resolve(ctx =>
                {
                    var ret = ctx.GetArgument<IEnumerable<IEnumerable<int?>>>("values"); //new int?[][] { new int?[] { 1, 2 }, new int?[] { 4, 5, 6 } };
                    var ret2 = ret.Select(x => new SimpleDataLoader<IEnumerable<SimpleDataLoader<int?>>>(_ => Task.FromResult(x.Select(y => new SimpleDataLoader<int?>(_ => Task.FromResult(y))))));
                    return ret2;
                });
        }
    }
}
