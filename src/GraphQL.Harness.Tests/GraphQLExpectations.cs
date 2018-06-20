using Alba;

namespace GraphQL.Harness.Tests
{
    public class GraphQLExpectations
    {
        private readonly Scenario _scenario;

        public GraphQLExpectations(Scenario scenario)
        {
            _scenario = scenario;
        }

        public GraphQLExpectations ShouldBeSuccess(string result)
        {
            _scenario.AssertThat(new SuccessResultAssertion(result));
            return this;
        }
    }
}