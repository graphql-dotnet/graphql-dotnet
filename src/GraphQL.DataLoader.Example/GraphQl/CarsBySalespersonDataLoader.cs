using DataLoaderGql.Types;
using GraphQL.DataLoader;
using Microsoft.EntityFrameworkCore;

namespace DataLoaderGql.GraphQl;

public class CarsBySalespersonDataLoader(DealershipDbContext db): DataLoaderBase<int, List<Car>>
{
    protected override async Task FetchAsync(IEnumerable<DataLoaderPair<int, List<Car>>> list, CancellationToken cancellationToken)
    {
        var keys = list.Select(pair => pair.Key).ToHashSet();
        var carsLookup = 
            await db.Cars.Where(car => keys.Contains(car.SalesPersonId))
                .GroupBy(car => car.SalesPersonId)
                .ToDictionaryAsync(
                    group => group.Key,
                    cancellationToken: cancellationToken);
        
        
        foreach (var pair in list)
        {
            pair.SetResult(carsLookup[pair.Key].ToList());
        }
    }
}