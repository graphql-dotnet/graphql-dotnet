using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Types;

// A C# union is a compiler generated struct carrying one case at a time, which is the same shape a GraphQL
// union describes, so each case type becomes a possible type. See CompilerMarkers.cs for why these compile
// on every target framework here.
public class CSharpUnionTests
{
    [Fact]
    public void MapsToGraphQLUnion()
    {
        var schema = BuildSchema<PetQuery>();

        schema.Print().ShouldBeCrossPlat("""
            schema {
              query: PetQuery
            }

            type PetQuery {
              pet: PetUnion!
              pets: [PetUnion!]!
            }

            union PetUnion = Cat | Dog

            type Cat {
              name: String!
              indoor: Boolean!
            }

            type Dog {
              name: String!
              goodBoy: Boolean!
            }

            """);
    }

    [Fact]
    public async Task ResolvesTheActiveCase()
    {
        var result = await ExecuteAsync<PetQuery>("{ pet { __typename ... on Cat { name indoor } ... on Dog { name } } }");

        result.ShouldBe("""{"data":{"pet":{"__typename":"Cat","name":"Tibbles","indoor":true}}}""");
    }

    // list items are completed through the same path as a single field, so each item is unwrapped separately
    [Fact]
    public async Task ResolvesEachItemOfAList()
    {
        var result = await ExecuteAsync<PetQuery>("{ pets { __typename ... on Cat { name } ... on Dog { name } } }");

        result.ShouldBe("""{"data":{"pets":[{"__typename":"Cat","name":"Tibbles"},{"__typename":"Dog","name":"Rex"}]}}""");
    }

    // a union is a struct, so a field returning one is non-null; holding no case is only expressible through
    // a nullable union, and reads as null rather than as a missing object type
    [Fact]
    public async Task UnionHoldingNoCaseIsNull()
    {
        var result = await ExecuteAsync<EmptyPetQuery>("{ pet { __typename } }");

        result.ShouldBe("""{"data":{"pet":null}}""");
    }

    [Fact]
    public void PossibleTypesAreTheCaseTypes()
    {
        var graphType = new AutoRegisteringUnionGraphType<PetUnion>();

        graphType.Name.ShouldBe("PetUnion");
        graphType.Types.ShouldBe([typeof(GraphQLClrOutputTypeReference<Cat>), typeof(GraphQLClrOutputTypeReference<Dog>)]);
    }

    [Fact]
    public void AppliesAttributesOfTheUnionType()
    {
        var graphType = new AutoRegisteringUnionGraphType<RenamedUnion>();

        graphType.Name.ShouldBe("Animal");
        graphType.Description.ShouldBe("A cat or a dog");
    }

    [Fact]
    public void RejectsATypeThatIsNotAUnion()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new AutoRegisteringUnionGraphType<Cat>())
            .Message.ShouldStartWith("The type 'Cat' is not a C# union type.");
    }

    // GraphQL unions hold object types only, so a case that maps to a scalar cannot be represented at all
    [Fact]
    public void ReportsACaseThatIsNotAnObjectType()
    {
        Should.Throw<InvalidOperationException>(() => BuildSchema<ScalarCaseQuery>())
            .Message.ShouldBe("The GraphQL type 'GraphQLClrOutputTypeReference<Int32>' for union graph type 'IntOrString' could not be derived implicitly. The resolved type is not an IObjectGraphType. A GraphQL union may only contain object types, so a scalar, enumeration or list cannot be one of its possible types.");
    }

    // GraphQL has no union input type, so this is reported rather than silently registered as an input object
    // with a single 'value' field of type Object, which is what it looks like through plain reflection
    [Fact]
    public void ReportsAUnionUsedAsAnInputType()
    {
        Should.Throw<InvalidOperationException>(() => BuildSchema<UnionArgumentQuery>())
            .Message.ShouldContain("The union type 'PetUnion' cannot be used as an input type, because GraphQL has no union input type.");
    }

    // pins the metadata the library matches on; it cannot reference the attribute, since it targets
    // frameworks that predate it
    [Fact]
    public void UnionIsMarkedWithTheExpectedAttribute()
    {
        typeof(PetUnion).GetCustomAttributesData()
            .Select(_ => _.AttributeType.FullName)
            .ShouldContain("System.Runtime.CompilerServices.UnionAttribute");
    }

    private static ISchema BuildSchema<TQuery>()
        where TQuery : class
    {
        var services = new ServiceCollection();
        services.AddGraphQL(_ => _.AddAutoSchema<TQuery>().AddSystemTextJson());
        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        return schema;
    }

    private static async Task<string> ExecuteAsync<TQuery>(string query)
        where TQuery : class
    {
        var services = new ServiceCollection();
        services.AddGraphQL(_ => _.AddAutoSchema<TQuery>().AddSystemTextJson());
        var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<IDocumentExecuter>().ExecuteAsync(_ =>
        {
            _.RequestServices = provider;
            _.Schema = provider.GetRequiredService<ISchema>();
            _.Query = query;
        });

        return provider.GetRequiredService<IGraphQLTextSerializer>().Serialize(result);
    }

    private class PetQuery
    {
        public static PetUnion Pet => new(new Cat("Tibbles", true));

        public static List<PetUnion> Pets => [new(new Cat("Tibbles", true)), new(new Dog("Rex", true))];
    }

    private class EmptyPetQuery
    {
        public static PetUnion? Pet => default(PetUnion);
    }

    private class ScalarCaseQuery
    {
        public static IntOrString Value => new(42);
    }

    private class UnionArgumentQuery
    {
        public static string Describe(PetUnion pet) => pet.ToString()!;
    }

    public record Cat(string Name, bool Indoor);

    public record Dog(string Name, bool GoodBoy);

    public union PetUnion(Cat, Dog);

    public union IntOrString(int, string);

    [Name("Animal")]
    [System.ComponentModel.Description("A cat or a dog")]
    public union RenamedUnion(Cat, Dog);
}
