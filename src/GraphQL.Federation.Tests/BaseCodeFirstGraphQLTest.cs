using GraphQL.Federation.Tests.Fixtures;

namespace GraphQL.Federation.Tests;

public abstract class BaseCodeFirstGraphQLTest : BaseTest
{
    protected readonly IServiceProvider Services;
    protected readonly GraphQL.Types.Schema Schema;

    protected BaseCodeFirstGraphQLTest(CodeFirstFixture codeFirstFixture)
    {
        Services = codeFirstFixture.Services;
        Schema = codeFirstFixture.Schema;
    }
}
