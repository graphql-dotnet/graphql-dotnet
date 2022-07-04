using GraphQL.Types;

namespace GraphQL.Tests.Utilities;

public class SchemaBuilderNestedTypesTests : SchemaBuilderTestBase
{
    [Fact]
    public void supports_nested_graph_types()
    {
        var defs = @"
              type Droid {
                  id: String
                  name: String
                  friend: Character
              }

              type Character {
                  name: String
              }

              type Query {
                  hero: Droid
              }
            ";

        Builder.Types.Include<DroidType, Droid>("Droid");
        Builder.Types.Include<Query>();

        var query = @"{ hero { id name friend { name } } }";
        var expected = @"{ ""hero"": { ""id"" : ""1"", ""name"": ""R2-D2"", ""friend"": { ""name"": ""C3-PO"" } } }";

        AssertQuery(_ =>
        {
            _.Query = query;
            _.Definitions = defs;
            _.ExpectedResult = expected;
        });
    }

    [Fact]
    public void supports_type_references_in_resolve_type()
    {
        var defs = @"
              type Droid {
                  id: String
                  name: String
                  friend: Character
              }

              type Character {
                  name: String
              }

              type Query {
                  hero: Droid
              }
            ";

        Builder.Types.Include<DroidType>("Droid");
        Builder.Types.For("Droid").ResolveType = obj => new GraphQLTypeReference("Droid");
        Builder.Types.Include<Query>();

        var query = @"{ hero { id name friend { name } } }";
        var expected = @"{ ""hero"": { ""id"" : ""1"", ""name"": ""R2-D2"", ""friend"": { ""name"": ""C3-PO"" } } }";

        AssertQuery(_ =>
        {
            _.Query = query;
            _.Definitions = defs;
            _.ExpectedResult = expected;
        });
    }

    public class Droid
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Character
    {
        public string Name { get; set; }
    }

    // [GraphQLMetadata("Droid", IsTypeOf = typeof(Droid))]
    public class DroidType
    {
        public string Id([FromSource] Droid droid) => droid.Id;

        public string Name([FromSource] Droid droid) => droid.Name;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
        public Character Friend()
        {
            return new Character { Name = "C3-PO" };
        }
    }

    public class Query
    {
        [GraphQLMetadata("hero")]
        public Droid GetHero()
        {
            return new Droid { Id = "1", Name = "R2-D2" };
        }
    }
}
