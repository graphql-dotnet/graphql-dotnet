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
            await run(_ =>
            {
                var input = new GraphQLRequest
                {
                    Query = @"{ hero { name} }"
                };
                _.Post.Json(input).ToUrl("/api/graphql");
                _.StatusCodeShouldBe(HttpStatusCode.OK);
                _.GraphQL().ShouldBeSuccess(@"{ ""hero"": { ""name"": ""R2-D2"" }}");
            });
        }
    }
}
