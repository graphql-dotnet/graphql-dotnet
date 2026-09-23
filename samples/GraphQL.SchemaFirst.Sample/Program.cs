using System.Text.Json;
using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Types;

Console.WriteLine("GraphQL.NET Schema-First sample");
Console.WriteLine();

var schema = Schema.For(
    """
    type Droid {
      id: String!
      name: String!
    }

    type Query {
      hero: Droid
    }
    """,
    builder => builder.Types.Include<Query>());

var json = await schema.ExecuteAsync(options =>
{
    options.Query = "{ hero { id name } }";
}).ConfigureAwait(false);

Console.WriteLine(json);

using var document = JsonDocument.Parse(json);
var hero = document.RootElement.GetProperty("data").GetProperty("hero");

if (hero.GetProperty("id").GetString() != "1" ||
    hero.GetProperty("name").GetString() != "R2-D2")
{
    Console.WriteLine("Unexpected schema-first sample result.");
    return 1;
}

return 0;

public sealed class Query
{
    [GraphQLMetadata("hero")]
    public Droid GetHero() => new() { Id = "1", Name = "R2-D2" };
}

public sealed class Droid
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
}
