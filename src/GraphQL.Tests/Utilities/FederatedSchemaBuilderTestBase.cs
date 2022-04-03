using GraphQL.Utilities.Federation;

namespace GraphQL.Tests.Utilities;

public class FederatedSchemaBuilderTestBase : SchemaBuilderTestBase
{
    public FederatedSchemaBuilderTestBase()
    {
        Builder = new FederatedSchemaBuilder();
    }
}
