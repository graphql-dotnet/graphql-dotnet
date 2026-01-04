using System.Net;
using GraphQL.Transport;

namespace GraphQL.Harness.SchemaFirst.Tests;

public class FragmentTests : SystemTestBase<Startup>
{
    [Fact]
    public async Task inline_fragments()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    {
                      hero {
                        name
                        ... on Droid {
                          primaryFunction
                        }
                        ... on Human {
                          homePlanet
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
                    "name": "R2-D2",
                    "primaryFunction": "Astromech"
                  }
                }
                """);
        });
    }

    [Fact]
    public async Task named_fragments()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    query {
                      human(id: "1") {
                        ...humanFields
                      }
                    }
                    
                    fragment humanFields on Human {
                      name
                      homePlanet
                    }
                    """
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""
                {
                  "human": {
                    "name": "Luke",
                    "homePlanet": "Tatooine"
                  }
                }
                """);
        });
    }

    [Fact]
    public async Task fragment_with_interface()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = """
                    query {
                      hero {
                        ...characterFields
                      }
                    }
                    
                    fragment characterFields on Character {
                      name
                      appearsIn
                    }
                    """
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""
                {
                  "hero": {
                    "name": "R2-D2",
                    "appearsIn": ["NEWHOPE", "EMPIRE", "JEDI"]
                  }
                }
                """);
        });
    }
}
