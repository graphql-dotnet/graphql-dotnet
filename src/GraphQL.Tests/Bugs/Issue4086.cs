using GraphQL.Federation;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Bugs;

public class Issue4086
{
    [Fact]
    public async Task ArgumentOnFederatedEntity()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema(BuildSchema)
            .AddFederation("2.3"));
        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = """
                {
                  _entities(representations: [{ __typename: "Droid", id: "123" }]) {
                    ... on Droid {
                      id
                      greet(name: "smith")
                    }
                  }
                }
                """;
            _.ThrowOnUnhandledException = true;
        });
        result.ShouldBeCrossPlatJson("""
            {
              "data": {
                "_entities": [
                  {
                    "id": "123",
                    "greet": "Hello, smith!"
                  }
                ]
              }
            }
            """);
    }

    private static ISchema BuildSchema(IServiceProvider serviceProvider)
    {
        var sdl = """
            type Droid @key(fields: "id") {
                id: ID!
                greet(name: String!): String
            }

            type Query {
                hero: Droid
            }
            """;

        var schemaBuilder = new GraphQL.Utilities.SchemaBuilder
        {
            ServiceProvider = serviceProvider,
        };
        schemaBuilder.Types.Include<Query>();
        schemaBuilder.Types.Include<Droid>();
        schemaBuilder.Types.For(nameof(Droid))
            .ResolveReference<Droid>((ctx, droid) => droid);

        return schemaBuilder.Build(sdl);
    }

    public class Query
    {
        public Droid Hero() => new Droid { Id = "123" };
    }

    public class Droid
    {
        public string Id { get; set; }
        public string Greet(string name) => $"Hello, {name}!";
    }
}
