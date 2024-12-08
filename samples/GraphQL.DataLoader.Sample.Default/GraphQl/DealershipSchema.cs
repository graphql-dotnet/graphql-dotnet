using GraphQL.Types;

namespace GraphQL.DataLoader.Sample.Default.GraphQL;

public class DealershipSchema : Schema
{
    public DealershipSchema(DealershipQuery query)
    {
        Query = query;
    }
}
