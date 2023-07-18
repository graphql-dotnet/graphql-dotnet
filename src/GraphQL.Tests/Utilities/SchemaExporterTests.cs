using GraphQL.Types;
using GraphQL.Utilities.Federation;
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

        schema.Print()
            .ShouldMatchApproved(o => o.NoDiff().WithFileExtension("defaults.txt"));
        schema.Print(new() { IncludeDeprecationReasons = false })
            .ShouldMatchApproved(o => o.NoDiff().WithFileExtension("noreasons.txt"));
        schema.Print(new() { IncludeDescriptions = false })
            .ShouldMatchApproved(o => o.NoDiff().WithFileExtension("nodescriptions.txt"));
        schema.Print(new() { StringComparison = StringComparison.InvariantCultureIgnoreCase })
            .ShouldMatchApproved(o => o.NoDiff().WithFileExtension("sorted.txt"));
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

    [Fact]
    public void Federation1Schema()
    {
        var schema = new FederatedSchemaBuilder()
            .Build("Federated".ReadSDL());
        schema.Print(new GraphQL.Utilities.PrintOptions { IncludeFederationTypes = false })
            .ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void Federation2Schema()
    {
        var schema = new FederatedSchemaBuilder()
            .Build("Federated".ReadSDL());
        schema.Print().ShouldMatchApproved(o => o.NoDiff());
    }
}
