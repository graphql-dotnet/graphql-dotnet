using System.Net;
using System.Threading.Tasks;
using Example;
using Xunit;

namespace GraphQL.Harness.Tests
{
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
            });
        }
    }
}
