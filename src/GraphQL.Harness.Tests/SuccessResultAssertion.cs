using Alba;
using GraphQL.Http;

namespace GraphQL.Harness.Tests
{
    public class SuccessResultAssertion : GraphQLAssertion
    {
        private readonly string _result;

        public SuccessResultAssertion(string result)
        {
            _result = result;
        }

        public override void Assert(Scenario scenario, ScenarioAssertionException ex)
        {
            var writer = (IDocumentWriter)scenario.Context.RequestServices.GetService(typeof(IDocumentWriter));
            var expectedResult = writer.WriteToStringAsync(CreateQueryResult(_result)).GetAwaiter().GetResult();

            var body = ex.ReadBody(scenario);
            if (!body.Equals(expectedResult))
            {
                ex.Add($"Expected '{expectedResult}' but got '{body}'");
            }
        }
    }
}
