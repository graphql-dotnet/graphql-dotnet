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
        schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase }).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void Federation1Schema()
    {
        var schema = new FederatedSchemaBuilder()
            .Build("Federated".ReadSDL());
        schema.Print(new() { IncludeFederationTypes = false, StringComparison = StringComparison.OrdinalIgnoreCase })
            .ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void Federation2Schema()
    {
        var schema = new FederatedSchemaBuilder()
            .Build("Federated".ReadSDL());
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

    [Fact]
    public void Print_Should_Remove_Input_Value_Deprecation_When_Feature_Disabled()
    {
        var schema = new TestSchema();
        schema.Features.DeprecationOfInputValues = false;
        schema.Initialize();

        schema.Print().ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void Print_Should_Keep_Input_Value_Deprecation_When_Feature_Enabled()
    {
        var schema = new TestSchema();
        schema.Features.DeprecationOfInputValues = true;
        schema.Initialize();

        schema.Print().ShouldMatchApproved(o => o.NoDiff());
    }

    private class TestSchema : Schema
    {
        public TestSchema()
        {
            Query = new QueryType();
        }

        private class QueryType : ObjectGraphType
        {
            public QueryType()
            {
                Name = "Query";
                Field<StringGraphType>("test")
                    .Argument<StringGraphType>("oldArg", arg => arg.DeprecationReason = "Use newArg instead")
                    .Argument<TestInputType>("input")
                    .Resolve(_ => "test");
            }
        }

        private class TestInputType : InputObjectGraphType
        {
            public TestInputType()
            {
                Name = "TestInput";
                Field<StringGraphType>("oldField").DeprecationReason("Use newField instead");
                Field<StringGraphType>("newField");
            }
        }
    }

    [Fact]
    public void Print_Should_Keep_Field_Deprecation_When_Input_Values_Feature_Disabled()
    {
        var schema = new TestSchemaWithFieldDeprecation();
        schema.Features.DeprecationOfInputValues = false;
        schema.Initialize();

        schema.Print().ShouldMatchApproved(o => o.NoDiff());
    }

    private class TestSchemaWithFieldDeprecation : Schema
    {
        public TestSchemaWithFieldDeprecation()
        {
            Query = new QueryType();
        }

        private class QueryType : ObjectGraphType
        {
            public QueryType()
            {
                Name = "Query";
                Field<StringGraphType>("oldField")
                    .DeprecationReason("Use newField instead")
                    .Resolve(_ => "test");
                Field<StringGraphType>("newField").Resolve(_ => "test");
            }
        }
    }

    [Fact]
    public void Print_Should_Remove_Only_Input_Value_Deprecation_When_Both_Present()
    {
        var schema = new TestSchemaWithMixedDeprecation();
        schema.Features.DeprecationOfInputValues = false;
        schema.Initialize();

        schema.Print().ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void Print_Should_Remove_All_DeprecationReasons_When_DeprecationOfInputValues_True_And_IncludeDeprecationReasons_False()
    {
        var schema = new TestSchemaWithMixedDeprecation();
        schema.Features.DeprecationOfInputValues = true;
        schema.Initialize();

        var options = new GraphQL.Utilities.PrintOptions { IncludeDeprecationReasons = false };
        schema.Print(options).ShouldMatchApproved(o => o.NoDiff());
    }

    private class TestSchemaWithMixedDeprecation : Schema
    {
        public TestSchemaWithMixedDeprecation()
        {
            Query = new QueryType();
        }

        private class QueryType : ObjectGraphType
        {
            public QueryType()
            {
                Name = "Query";
                Field<StringGraphType>("oldOutputField")
                    .DeprecationReason("Use newField instead")
                    .Argument<StringGraphType>("oldArg", arg => arg.DeprecationReason = "Use newArg instead")
                    .Resolve(_ => "test");
            }
        }
    }
}
