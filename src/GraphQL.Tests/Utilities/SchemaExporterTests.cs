using GraphQL.Types;
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
        schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase }).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void Federation1Schema()
    {
        // this prints the schema without federation types, which is typical for federation v1 schemas
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(b => b
            .AddSchema(provider => Schema.For("Federated".ReadSDL(), c => c.ServiceProvider = provider))
            .AddFederation("1.0"));
        var services = serviceCollection.BuildServiceProvider();
        var schema = services.GetRequiredService<ISchema>();
        schema.Print(new() { IncludeFederationTypes = false, StringComparison = StringComparison.OrdinalIgnoreCase })
            .ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void Federation1Schema_WithFederationTypes()
    {
        // this prints the schema with federation v1 types defined, which is the default for greater compatibility
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(b => b
            .AddSchema(provider => Schema.For("Federated".ReadSDL(), c => c.ServiceProvider = provider))
            .AddFederation("1.0"));
        var services = serviceCollection.BuildServiceProvider();
        var schema = services.GetRequiredService<ISchema>();
        schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase }).ShouldMatchApproved(o => o.NoDiff());
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

    [Fact]
    public void PrintsOneOfTypesCorrectly()
    {
        var sdl = """
            input ExampleInputTagged @oneOf {
              a: String
              b: Int
            }
            
            type Query {
              test(arg: ExampleInputTagged!): String
            }

            """;
        var schema = Schema.For(sdl);
        var exported = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });
        exported.ShouldBe(sdl);
    }

    [Fact]
    public void EndsWithNewline()
    {
        var schema = Schema.For(
            "PetComplex".ReadSDL(),
            builder =>
            {
                builder.Types.ForAll(config => config.ResolveType = _ => null!);
                builder.IgnoreComments = false;
            }
        );
        var exported = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });
        exported.ShouldEndWith(Environment.NewLine);
    }
}
