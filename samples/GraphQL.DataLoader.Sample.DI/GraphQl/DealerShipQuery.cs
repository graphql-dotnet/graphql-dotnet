using GraphQL.Types;

namespace GraphQL.DataLoader.Sample.DI.GraphQL;

public sealed class DealershipQuery : ObjectGraphType
{
    public DealershipQuery()
    {
        Field<SalespersonGraphType>("salespeople")
            .Argument<string>("name")
            .Resolve(ctx =>
        {
            var name = ctx.GetArgument<string>("name");
            var loader = ctx.RequestServices!.GetRequiredService<SalespeopleByNameDataLoader>();
            return loader.LoadAsync(name);
        });
    }
}
