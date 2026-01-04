using System.Net;
using GraphQL.Transport;

namespace GraphQL.Harness.SchemaFirst.Tests;

public class MutationTests : SystemTestBase<Startup>
{
    [Fact]
    public async Task create_human()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = "mutation ($human:HumanInput!){ createHuman(human: $human) { name homePlanet } }",
                Variables = new Inputs(new Dictionary<string, object?>
                {
                    {
                        "human",
                        new Dictionary<string, object?>
                        {
                            {"name", "Boba Fett"},
                            {"homePlanet", "Kamino"}
                        }
                    }
                })
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""
                {
                  "createHuman": {
                    "name": "Boba Fett",
                    "homePlanet": "Kamino"
                  }
                }
                """);
        });
    }

    [Fact]
    public async Task create_human_without_home_planet()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = "mutation ($human:HumanInput!){ createHuman(human: $human) { name homePlanet } }",
                Variables = new Inputs(new Dictionary<string, object?>
                {
                    {
                        "human",
                        new Dictionary<string, object?>
                        {
                            {"name", "Rey"}
                        }
                    }
                })
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess("""
                {
                  "createHuman": {
                    "name": "Rey",
                    "homePlanet": null
                  }
                }
                """);
        });
    }
}
