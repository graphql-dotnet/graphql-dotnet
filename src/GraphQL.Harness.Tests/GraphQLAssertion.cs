using Alba;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace GraphQL.Harness.Tests
{
    public abstract class GraphQLAssertion : IScenarioAssertion
    {
        public abstract void Assert(Scenario scenario, HttpContext context, ScenarioAssertionException ex);

        protected ExecutionResult CreateQueryResult(string result, ExecutionErrors errors = null)
        {
            object data = null;
            if (!string.IsNullOrWhiteSpace(result))
            {
                data = JObject.Parse(result);
            }

            return new ExecutionResult { Data = data, Errors = errors };
        }
    }
}
