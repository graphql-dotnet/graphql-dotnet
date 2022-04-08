using System.Net;
using GraphQL.Transport;

namespace GraphQL.Harness.Tests;

public class BasicTests : SystemTestBase<Startup>
{
    [Fact]
    public async Task hero()
    {
        await run(scenario =>
        {
            var input = new GraphQLRequest
            {
                Query = @"{ hero { name} }"
            };
            scenario.Post.Json(input).ToUrl("/graphql");
            scenario.StatusCodeShouldBe(HttpStatusCode.OK);
            scenario.GraphQL().ShouldBeSuccess(@"{ ""hero"": { ""name"": ""R2-D2"" }}");
        }).ConfigureAwait(false);
    }
}
