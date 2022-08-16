using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types;

public class QueryType : ObjectGraphType
{
    public QueryType(IDataLoaderContextAccessor accessor, IUsersStore users, IOrdersStore orders)
    {
        Name = "Query";

        Field<ListGraphType<UserType>, IEnumerable<User>>("Users")
            .Description("Get all Users")
            .ResolveAsync(ctx =>
            {
                var loader = accessor.Context.GetOrAddLoader("GetAllUsers",
                    users.GetAllUsersAsync);

                return loader.LoadAsync();
            });

        Field<ListGraphType<UserType>, IEnumerable<User>>("UsersWithDelay")
            .Description("Get all Users")
            .ResolveAsync(async ctx =>
            {
                await System.Threading.Tasks.Task.Delay(20).ConfigureAwait(false);

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

        Field<NonNullGraphType<ListGraphType<UserType>>, IDataLoaderResult<IEnumerable<User>>>()
            .Name("SpecifiedUsersWithThen")
            .Description("Get Users by ID skipping null matches")
            .Argument<NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>>("ids")
            .Resolve(ctx =>
            {
                var loader = accessor.Context.GetOrAddBatchLoader<int, User>("GetUserById",
                    users.GetUsersByIdAsync);

                var ids = ctx.GetArgument<IEnumerable<int>>("ids");
                // note: does not work properly without ToList, because LoadAsync would not have
                // been called, so the ids would not have been queued for execution prior to the
                // first call to GetResultAsync
                var ret = ids.Select(id => loader.LoadAsync(id)).ToList();
                var ret2 = ret.Then(values => values.Where(x => x != null));
                return ret2;
            });

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>>>>("ExerciseListsOfLists")
            .Argument<ListGraphType<ListGraphType<IntGraphType>>>("values")
            .Resolve(ctx =>
            {
                var ret = ctx.GetArgument<IEnumerable<IEnumerable<int?>>>("values"); //new int?[][] { new int?[] { 1, 2 }, new int?[] { 4, 5, 6 } };
                var ret2 = ret.Select(x => new SimpleDataLoader<IEnumerable<SimpleDataLoader<int?>>>(_ => Task.FromResult(x.Select(y => new SimpleDataLoader<int?>(_ => Task.FromResult(y))))));
                return ret2;
            });
    }
}
