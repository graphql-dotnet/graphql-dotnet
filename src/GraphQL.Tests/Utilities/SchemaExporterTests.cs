using GraphQL.Types;
using GraphQL.Utilities.Federation;
using GraphQLParser.AST;
using Microsoft.Extensions.DependencyInjection;
using SchemaExporter = GraphQL.Utilities.SchemaExporter;

namespace GraphQL.Tests.Utilities;

public class SchemaExporterTests
{
    [Theory]
    [InlineData(SampleVariation.Defaults)]
    [InlineData(SampleVariation.NoReasons)]
    [InlineData(SampleVariation.NoDescriptions)]
    [InlineData(SampleVariation.Sorted)]
    public void PetComplex(SampleVariation variation)
    {
        var schema = Schema.For(
            "PetComplex".ReadSDL(),
            builder =>
            {
                builder.Types.ForAll(config => config.ResolveType = _ => null!);
                builder.IgnoreComments = false;
            }
        );

        var opts = new GraphQL.Utilities.PrintOptions();
        if (variation == SampleVariation.NoDescriptions)
            opts.IncludeDescriptions = false;
        if (variation == SampleVariation.NoReasons)
            opts.IncludeDeprecationReasons = false;
        if (variation == SampleVariation.Sorted)
            opts.StringComparison = StringComparison.InvariantCultureIgnoreCase;
        schema.Print(opts)
            .ShouldMatchApproved(o => o.NoDiff().WithDiscriminator(variation.ToString()));
    }

    public enum SampleVariation
    {
        Defaults,
        NoReasons,
        NoDescriptions,
        Sorted,
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

    [Fact]
    public void DoesntPrintSchema()
    {
        var schema = Schema.For("""
            schema {
              query: Query
            }
            type Query {
              hello: String
            }
            """);
        var exported = new SchemaExporter(schema).Export();
        exported.Definitions.Count(x => x is GraphQLSchemaDefinition).ShouldBe(0);
    }

    [Fact]
    public void DoesntPrintSchema2()
    {
        var schema = Schema.For("""
            schema {
              query: Query
              mutation: Mutation
            }
            type Query {
              hello: String
            }
            type Mutation {
              hello: String
            }
            """);
        var exported = new SchemaExporter(schema).Export();
        exported.Definitions.Count(x => x is GraphQLSchemaDefinition).ShouldBe(0);
    }

    [Fact]
    public void PrintsSchemaWithDescription()
    {
        var schema = Schema.For("""
            "sample"
            schema {
              query: Query
            }
            type Query {
              hello: String
            }
            """);
        var exported = new SchemaExporter(schema).Export();
        exported.Definitions.Count(x => x is GraphQLSchemaDefinition).ShouldBe(1);
    }

    [Fact]
    public void PrintsSchemaWithDirective()
    {
        var schema = Schema.For("""
            schema @test {
              query: Query
            }
            type Query {
              hello: String
            }
            directive @test on SCHEMA
            """);
        var exported = new SchemaExporter(schema).Export();
        exported.Definitions.Count(x => x is GraphQLSchemaDefinition).ShouldBe(1);
    }

    [Fact]
    public void PrintsSchemaWithAltType()
    {
        var schema = Schema.For("""
            schema {
              query: Query2
            }
            type Query2 {
              hello: String
            }
            """);
        var exported = new SchemaExporter(schema).Export();
        exported.Definitions.Count(x => x is GraphQLSchemaDefinition).ShouldBe(1);
    }

    [Fact]
    public void PrintsSchemaWhenMutationNotSpecified()
    {
        var schema = Schema.For("""
            schema {
              query: Query
            }
            type Query {
              hello: String
            }
            type Mutation {
              hello: String
            }
            """);
        var exported = new SchemaExporter(schema).Export();
        exported.Definitions.Count(x => x is GraphQLSchemaDefinition).ShouldBe(1);
    }
}
