using GraphQL.DataLoader.Sample.Default.Types;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.DataLoader.Sample.Default.GraphQL;

public sealed class DealershipQuery : ObjectGraphType
{
    public DealershipQuery(IDataLoaderContextAccessor accessor)
    {
        Field<Salesperson>("salesPerson", true)
            .Argument<string>("name")
            .ResolveAsync(ctx =>
            {
                var dbContext = ctx.RequestServices!.GetRequiredService<DealershipDbContext>();
                var loader = accessor.Context.GetOrAddBatchLoader<string, Salesperson>("GetSalespeopleByName", async names => await dbContext.Salespeople.Where(s => names.Contains(s.Name)).ToDictionaryAsync(sp => sp.Name));
                return loader.LoadAsync(ctx.GetArgument<string>("name"));
            });
    }
}
