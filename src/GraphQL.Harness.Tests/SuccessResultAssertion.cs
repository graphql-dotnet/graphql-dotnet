using Alba;
using GraphQL.Http;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Harness.Tests
{
    public class SuccessResultAssertion : GraphQLAssertion
    {
        private readonly string _result;

        public SuccessResultAssertion(string result)
        {
            _result = result;
        }

        public override void Assert(Scenario scenario, HttpContext context, ScenarioAssertionException ex)
        {
            var writer = new DocumentWriter();
            var expectedResult = writer.WriteToStringAsync(CreateQueryResult(_result)).GetAwaiter().GetResult();

            var body = ex.ReadBody(context);
            if (!body.Equals(expectedResult))
            {
                ex.Add($"Expected '{expectedResult}' but got '{body}'");
            }
        }
    }
}
