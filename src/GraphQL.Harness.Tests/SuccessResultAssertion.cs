using Alba;
using GraphQL.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace GraphQL.Harness.Tests
{
    public class SuccessResultAssertion : GraphQLAssertion
    {
        private readonly string _result;
        private readonly bool _ignoreExtensions;

        public SuccessResultAssertion(string result, bool ignoreExtensions)
        {
            _result = result;
            _ignoreExtensions = ignoreExtensions;
        }

        public override void Assert(Scenario scenario, HttpContext context, ScenarioAssertionException ex)
        {
            var writer = new DocumentWriter();
            var expectedResult = writer.WriteToStringAsync(CreateQueryResult(_result)).GetAwaiter().GetResult();

            var body = ex.ReadBody(context);

            if (!body.Equals(expectedResult))
            {
                if (_ignoreExtensions)
                {
                    var json = JObject.Parse(body);
                    json.Remove("extensions");
                    var bodyWithoutExtensions = json.ToString(Newtonsoft.Json.Formatting.None);
                    if (bodyWithoutExtensions.Equals(expectedResult))
                        return;
                }

                ex.Add($"Expected '{expectedResult}' but got '{body}'");
            }
        }
    }
}
