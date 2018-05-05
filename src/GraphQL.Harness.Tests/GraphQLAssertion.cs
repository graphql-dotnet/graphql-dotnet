using Alba;
using Newtonsoft.Json.Linq;

namespace GraphQL.Harness.Tests
{
    public abstract class GraphQLAssertion : IScenarioAssertion
    {
        public abstract void Assert(Scenario scenario, ScenarioAssertionException ex);

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
