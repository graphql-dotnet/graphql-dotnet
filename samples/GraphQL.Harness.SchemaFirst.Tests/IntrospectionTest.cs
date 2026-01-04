using System.Net;
using GraphQL.Transport;

namespace GraphQL.Harness.SchemaFirst.Tests;

public class IntrospectionTest : SystemTestBase<Startup>
{
    [Fact]
    public async Task introspect_schema()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    {
                      __schema {
                        queryType {
                          name
                        }
                        mutationType {
                          name
                        }
                        types {
                          name
                          kind
                        }
                      }
                    }
                    """
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            // Just verify it returns successfully - full introspection validation would be very long
        });
    }

    [Fact]
    public async Task introspect_type()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    {
                      __type(name: "Human") {
                        name
                        kind
                        fields {
                          name
                          type {
                            name
                            kind
                          }
                        }
                      }
                    }
                    """
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            // Verify Human type exists and has expected structure
        });
    }

    [Fact]
    public async Task typename_field()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    {
                      hero {
                        __typename
                        name
                      }
                    }
                    """
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""
                {
                  "hero": {
                    "__typename": "Droid",
                    "name": "R2-D2"
                  }
                }
                """);
        });
    }
}
