using GraphQL.DataLoader.Di.Sample.Types;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.DataLoader.Di.Sample.GraphQl;

public sealed class SalespersonGraphType : ObjectGraphType<Salesperson>
{
    public SalespersonGraphType(IDataLoaderContextAccessor accessor, DealershipDbContext dbContext)
    {
        Field(x => x.Id).Description("Id of the salesman");
        Field(x => x.Name).Description("Name of the salesman");

        Field<ListGraphType<CarsGraphType>, IEnumerable<Car>>("assignedCars").Description("Assigned cars")
            .ResolveAsync(ctx =>
            {
                var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, Car>("GetCarsBySalespeople", async salesPersonIds =>
                {
                    var carsLookup = (await dbContext.Cars.Where(car => salesPersonIds.Contains(car.SalesPersonId)).ToListAsync().ConfigureAwait(false)).ToLookup(car => car.SalesPersonId);
                    return carsLookup;
                });
                return loader.LoadAsync(ctx.Source.Id);
            });
    }
}
