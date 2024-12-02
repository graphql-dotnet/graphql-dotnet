using DataLoaderGql.Types;
using GraphQL.DataLoader;
using Microsoft.EntityFrameworkCore;

namespace DataLoaderGql.GraphQl;

public class CarsBySalespersonDataLoader(DealershipDbContext db) : DataLoaderBase<int, List<Car>>
{
    protected override async Task FetchAsync(IEnumerable<DataLoaderPair<int, List<Car>>> list, CancellationToken cancellationToken)
    {
        var keys = list.Select(pair => pair.Key);
        var carsLookup =
            (await db.Cars.Where(car
                => keys
                .Contains(car.SalesPersonId))
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
                .ToLookup(car => car.SalesPersonId);


        foreach (var pair in list)
        {
            pair.SetResult(carsLookup[pair.Key].ToList());
        }
    }
}
