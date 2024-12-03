using GraphQL.Types;

namespace GraphQL.DataLoader.Di.Sample.GraphQl;

public class DealershipSchema : Schema
{
    public DealershipSchema(IDataLoaderContextAccessor accessor, DealershipDbContext dbContext)
    {
        Query = new DealerShipQuery(accessor, dbContext);
    }
}
