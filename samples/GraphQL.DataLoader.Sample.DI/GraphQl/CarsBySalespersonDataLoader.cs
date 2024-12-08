using GraphQL.DataLoader.Sample.DI.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.DataLoader.Sample.DI.GraphQl;

public class CarsBySalespersonDataLoader(DealershipDbContext db) : DataLoaderBase<int, IEnumerable<Car>>
{
    protected override async Task FetchAsync(IEnumerable<DataLoaderPair<int, IEnumerable<Car>>> list, CancellationToken cancellationToken)
    {
        var keys = list.Select(pair => pair.Key);
        var carsLookup = (await db.Cars
            .Where(car => keys.Contains(car.SalesPersonId))
            .ToListAsync(cancellationToken: cancellationToken))
            .ToLookup(car => car.SalesPersonId);

        foreach (var pair in list)
        {
            pair.SetResult(carsLookup[pair.Key]);
        }
    }
}
