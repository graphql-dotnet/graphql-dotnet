using GraphQL.DataLoader.Sample.Default.Types;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.DataLoader.Sample.Default.GraphQL;

public sealed class SalespersonGraphType : ObjectGraphType<Salesperson>
{
    public SalespersonGraphType(IDataLoaderContextAccessor accessor)
    {
        Field(x => x.Id).Description("Id of the salesman");
        Field(x => x.Name).Description("Name of the salesman");

        Field<IEnumerable<Car>>("assignedCars", false).Description("Assigned cars")
            .ResolveAsync(ctx =>
            {
                var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, Car>("GetCarsBySalespeople", async salesPersonIds =>
                {
                    var dbContext = ctx.RequestServices!.GetRequiredService<DealershipDbContext>();
                    var carsLookup = (await dbContext.Cars.Where(car => salesPersonIds.Contains(car.SalesPersonId)).ToListAsync()).ToLookup(car => car.SalesPersonId);
                    return carsLookup;
                });
                return loader.LoadAsync(ctx.Source.Id);
            });
    }
}
