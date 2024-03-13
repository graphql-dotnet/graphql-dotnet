using GraphQL.Federation.Tests.Fixtures;

namespace GraphQL.Federation.Tests;

public abstract class BaseSchemaFirstGraphQLTest : BaseTest
{
    protected readonly IServiceProvider Services;
    protected readonly GraphQL.Types.Schema Schema;

    protected BaseSchemaFirstGraphQLTest(SchemaFirstFixture serviceProviderFixture)
    {
        Services = serviceProviderFixture.Services;
        Schema = serviceProviderFixture.Schema;
    }
}
