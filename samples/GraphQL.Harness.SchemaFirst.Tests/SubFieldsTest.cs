using System.Net;
using GraphQL.Transport;

namespace GraphQL.Harness.SchemaFirst.Tests;

public class SubFieldsTest : SystemTestBase<Startup>
{
    [Fact]
    public async Task nested_friends()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    {
                      human(id: "1") {
                        name
                        friends {
                          name
                          friends {
                            name
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
                    "friends": [
                      {
                        "name": "R2-D2",
                        "friends": [
                          {
                            "name": "Luke"
                          },
                          {
                            "name": "C-3PO"
                          }
                        ]
                      },
                      {
                        "name": "C-3PO",
                        "friends": null
                      }
                    ]
                  }
                }
                """);
        });
    }

    [Fact]
    public async Task appears_in_episodes()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    {
                      human(id: "1") {
                        name
                        appearsIn
                        friends {
                          name
                          appearsIn
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
                    "appearsIn": ["NEWHOPE", "EMPIRE", "JEDI"],
                    "friends": [
                      {
                        "name": "R2-D2",
                        "appearsIn": ["NEWHOPE", "EMPIRE", "JEDI"]
                      },
                      {
                        "name": "C-3PO",
                        "appearsIn": ["NEWHOPE", "EMPIRE", "JEDI"]
                      }
                    ]
                  }
                }
                """);
        });
    }
}
