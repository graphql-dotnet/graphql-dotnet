using GraphQL.Types;

namespace GraphQL.DataLoader.Sample.DI.GraphQl;

public class DealershipSchema : Schema
{
    public DealershipSchema()
    {
        Query = new DealerShipQuery();
    }
}
