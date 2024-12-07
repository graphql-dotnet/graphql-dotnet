using GraphQL.DataLoader.DI.Sample.Types;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.DataLoader.DI.Sample.GraphQl;

public sealed class DealerShipQuery : ObjectGraphType
{
    public DealerShipQuery(IDataLoaderContextAccessor accessor, DealershipDbContext dbContext)
    {
        Field<SalespersonGraphType>("salespeople")
            .Argument<string>("name")
            .Resolve(ctx =>
            {
                var loader = accessor.Context.GetOrAddBatchLoader<string, Salesperson>("GetSalespeopleByName", async names => await dbContext.Salespeople.Where(s => names.Contains(s.Name)).ToDictionaryAsync(sp => sp.Name));
                return loader.LoadAsync(ctx.GetArgument<string>("name"));
            });
    }
}
