using GraphQL.Types;

namespace GraphQL.DataLoader.DI.Sample.GraphQl;

public class DealershipSchema : Schema
{
    public DealershipSchema(IDataLoaderContextAccessor accessor, DealershipDbContext dbContext)
    {
        Query = new DealerShipQuery(accessor, dbContext);
    }
}
