using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Utilities;

public class SchemaExporterTests
{
    [Fact]
    public void PetComplex()
    {
        var schema = Schema.For(
            "PetComplex".ReadSDL(),
            builder =>
            {
                builder.Types.ForAll(config => config.ResolveType = _ => null);
                builder.IgnoreComments = false;
            }
        );

        schema.Print().ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void StarWarsSchema()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(b => b
            .AddSchema<GraphQL.StarWars.StarWarsSchema>()
            .AddGraphTypes(typeof(GraphQL.StarWars.StarWarsSchema).Assembly));
        serviceCollection.AddSingleton<GraphQL.StarWars.StarWarsData>();
        var services = serviceCollection.BuildServiceProvider();
        var schema = services.GetRequiredService<ISchema>();
        schema.Print().ShouldMatchApproved(o => o.NoDiff());
    }
}
