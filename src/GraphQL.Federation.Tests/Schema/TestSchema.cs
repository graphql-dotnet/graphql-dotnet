using GraphQL.Federation.Tests.Schema.Output;

namespace GraphQL.Federation.Tests.Schema;

public class TestSchema : GraphQL.Types.Schema
{
    public TestSchema(IServiceProvider provider, TestQuery query)
        : base(provider)
    {
        Query = query;

        this.RegisterType<FederatedTestType>();
    }
}
