using GraphQL.DataLoader.Sample.DI.Types;
using GraphQL.Types;

namespace GraphQL.DataLoader.Sample.DI.GraphQL;

public sealed class DealershipQuery : ObjectGraphType
{
    public DealershipQuery()
    {
        Field<Salesperson>("salespeople", true)
            .Argument<string>("name")
            .ResolveAsync(ctx =>
            {
                var name = ctx.GetArgument<string>("name");
                var loader = ctx.RequestServices!.GetRequiredService<SalespeopleByNameDataLoader>();
                return loader.LoadAsync(name);
            });
    }
}
