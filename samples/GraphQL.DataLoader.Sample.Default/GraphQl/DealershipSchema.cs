using GraphQL.Types;

namespace GraphQL.DataLoader.Di.Sample.GraphQl;

public class DealershipSchema : Schema
{
    public DealershipSchema()
    {
        Query = new DealerShipQuery();
    }
}
