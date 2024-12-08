using GraphQL.Federation.Tests.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Federation.Tests.Fixtures;

public class CodeFirstFixture
{
    public readonly IServiceProvider Services;
    public readonly GraphQL.Types.Schema Schema;


    public CodeFirstFixture()
    {
        var sc = new ServiceCollection();

        sc.AddGraphQL(builder => builder
            .AddSystemTextJson()
            .AddDataLoader()
            .AddSchema<TestSchema>()
            .AddGraphTypes(typeof(TestSchema).Assembly)
            .AddClrTypeMappings(typeof(TestSchema).Assembly)
            .AddAutoClrMappings()
            .AddFederation("2.3"));

        Services = sc.BuildServiceProvider();

        Schema = Services.GetRequiredService<TestSchema>();
    }
}
