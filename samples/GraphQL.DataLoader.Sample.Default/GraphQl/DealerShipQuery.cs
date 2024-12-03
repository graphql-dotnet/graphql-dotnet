using GraphQL;
using GraphQL.DataLoader.Di.Sample.Types;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.DataLoader.Di.Sample.GraphQl;

public sealed class DealerShipQuery : ObjectGraphType
{
    public DealerShipQuery(IDataLoaderContextAccessor accessor, DealershipDbContext dbContext)
    {
        Field<SalespersonGraphType>("salespeople")
            .Argument<string>("name")
            .Resolve(ctx => {
                var loader = accessor.Context.GetOrAddBatchLoader<string, Salesperson>("GetSalespeopleByName", async names =>
                {
                    return await dbContext.Salespeople.Where(s => names.Contains(s.Name)).ToDictionaryAsync(sp => sp.Name);
                });
                return loader.LoadAsync(ctx.GetArgument<string>("name"));
            });
    }
}
