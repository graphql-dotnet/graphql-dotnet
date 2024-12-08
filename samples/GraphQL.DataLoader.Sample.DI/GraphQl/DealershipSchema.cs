using GraphQL.Types;

namespace GraphQL.DataLoader.Sample.DI.GraphQL;

public class DealershipSchema : Schema
{
    public DealershipSchema(IServiceProvider serviceProvider, DealershipQuery query) : base(serviceProvider)
    {
        Query = query;
    }
}
