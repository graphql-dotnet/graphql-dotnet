using GraphQL.Types;

namespace GraphQL.DataLoader.Sample.Default.GraphQL;

public class DealershipSchema : Schema
{
    public DealershipSchema(IServiceProvider serviceProvider, DealershipQuery query) : base(serviceProvider)
    {
        Query = query;
    }
}
