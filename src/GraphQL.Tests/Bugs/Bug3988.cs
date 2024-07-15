using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/3988
public class Bug3988InlineFragmentSpreadWithoutTypeCondition
{
    [Fact]
    public async Task Test()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(b => b
            .AddAutoSchema<Query>()
            .AddSystemTextJson());
        using var services = serviceCollection.BuildServiceProvider();

        var executor = services.GetRequiredService<IDocumentExecuter<ISchema>>();
        var ret = await executor.ExecuteAsync(new ExecutionOptions
        {
            Query = """
                {
                  hero {              # hero returns a non-null type (User!)
                    __typename
                    id
                    ... {             # inline fragment spread without type condition
                      name
                    }
                    ... on User {     # inline fragment spread with type condition
                      name2: name
                    }
                    ...userFragment   # fragment spread
                  }
                }
                fragment userFragment on User {
                  name3: name
                }
                """,
            RequestServices = services,
        });

        var serializer = services.GetRequiredService<IGraphQLTextSerializer>();
        var json = serializer.Serialize(ret);
        json.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "hero": {
                        "__typename": "User",
                        "id": "00000000-0000-0000-0000-000000000001",
                        "name": "John Doe",
                        "name2": "John Doe",
                        "name3": "John Doe"
                    }
                }
            }
            """);
    }

    public class Query
    {
        public static User Hero => new();
    }

    public class User
    {
        [Id]
        public Guid Id => Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string Name => "John Doe";
    }
}
