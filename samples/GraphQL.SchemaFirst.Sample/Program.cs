using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Types;

namespace GraphQL.SchemaFirst.Sample;

public static class Program
{
    public static async Task Main()
    {
        using var schema = Schema.For(SchemaDefinition, builder =>
        {
            builder.Types.Include<Query>();
            builder.Types.Include<Droid>();
        });

        var json = await schema.ExecuteAsync(options =>
        {
            options.Query = """
                {
                  hero {
                    id
                    name
                    primaryFunction
                  }
                  droid(id: "2") {
                    name
                  }
                }
                """;
        });

        Console.WriteLine(json);
    }

    private const string SchemaDefinition = """
        type Droid {
          id: ID!
          name: String!
          primaryFunction: String!
        }

        type Query {
          hero: Droid!
          droid(id: ID!): Droid
        }
        """;
}

[GraphQLMetadata("Query")]
public sealed class Query
{
    private readonly IReadOnlyDictionary<string, Droid> _droids = new Dictionary<string, Droid>
    {
        ["1"] = new("1", "C-3PO", "Protocol"),
        ["2"] = new("2", "R2-D2", "Astromech"),
    };

    [GraphQLMetadata("hero")]
    public Droid GetHero() => _droids["2"];

    [GraphQLMetadata("droid")]
    public Droid? GetDroid(string id) => _droids.GetValueOrDefault(id);
}

[GraphQLMetadata("Droid")]
public sealed record Droid(
    string Id,
    string Name,
    string PrimaryFunction);
