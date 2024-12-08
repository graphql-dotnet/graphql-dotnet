using GraphQL.Types;

namespace GraphQL.DataLoader.Sample.DI.GraphQL;

public class DealershipSchema : Schema
{
    public DealershipSchema(DealershipQuery query)
    {
        Query = query;
    }
}
