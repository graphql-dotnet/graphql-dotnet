using GraphQL.Tests.Bugs.Configuration;

namespace GraphQL.Tests.Bugs
{
    public class Bug142EnumerationTests : AdvancedSchemaTestBase
    {
        [Fact]
        public void EnumerationMutation_WorksAsExpected()
        {
            var query = @"
                {
                  enumQuery(episode:NEWHOPE) {
                    episodeSet
                  }
                }
            ";

            var expected = @"{
              enumQuery: {
                episodeSet: ""NEWHOPE""
                }
            }";

            AssertQuerySuccess(query, expected);
        }
    }
}
