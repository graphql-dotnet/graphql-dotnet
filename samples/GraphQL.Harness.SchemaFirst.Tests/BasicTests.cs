using System.Net;
using GraphQL.Transport;

namespace GraphQL.Harness.SchemaFirst.Tests;

public class BasicTests : SystemTestBase<Startup>
{
    [Fact]
    public async Task hero()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = "{ hero { name} }"
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""{ "hero": { "name": "R2-D2" }}""");
        });
    }

    [Fact]
    public async Task human()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = "{ human(id: \"1\") { name homePlanet } }"
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""{ "human": { "name": "Luke", "homePlanet": "Tatooine" }}""");
        });
    }

    [Fact]
    public async Task droid()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = "{ droid(id: \"4\") { name } }"
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""{ "droid": { "name": "C-3PO" }}""");
        });
    }

    [Fact]
    public async Task hero_with_friends()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    {
                      hero {
                        id
                        name
                        friends {
                          name
                        }
                      }
                    }
                    """
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""
                {
                  "hero": {
                    "id": "3",
                    "name": "R2-D2",
                    "friends": [
                      {
                        "name": "Luke"
                      },
                      {
                        "name": "C-3PO"
                      }
                    ]
                  }
                }
                """);
        });
    }

    [Fact]
    public async Task query_with_variables()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = "query humanQuery($id: String!) { human(id: $id) { name } }",
                Variables = new Inputs(new Dictionary<string, object?> { { "id", "1" } })
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""{ "human": { "name": "Luke" }}""");
        });
    }

    [Fact]
    public async Task aliased_queries()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    query SomeDroids {
                      r2d2: droid(id: "3") {
                        name
                      }
                      c3po: droid(id: "4") {
                        name
                      }
                    }
                    """
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""
                {
                  "r2d2": {
                    "name": "R2-D2"
                  },
                  "c3po": {
                    "name": "C-3PO"
                  }
                }
                """);
        });
    }

    [Fact]
    public async Task friends_connection()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    {
                      human(id: "1") {
                        name
                        friendsConnection {
                          totalCount
                          edges {
                            node {
                              name
                            }
                            cursor
                          }
                          pageInfo {
                            endCursor
                            hasNextPage
                          }
                        }
                      }
                    }
                    """
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""
                {
                  "human": {
                    "name": "Luke",
                    "friendsConnection": {
                      "totalCount": 2,
                      "edges": [
                        {
                          "node": {
                            "name": "R2-D2"
                          },
                          "cursor": "Mw=="
                        },
                        {
                          "node": {
                            "name": "C-3PO"
                          },
                          "cursor": "NA=="
                        }
                      ],
                      "pageInfo": {
                        "endCursor": "NA==",
                        "hasNextPage": false
                      }
                    }
                  }
                }
                """);
        });
    }
}
