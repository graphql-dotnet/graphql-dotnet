using GraphQL.Types;

namespace DataLoaderGql.GraphQl;

public class DealershipSchema : Schema
{
    public DealershipSchema()
    {
        Query = new DealerShipQuery();
    }
}
