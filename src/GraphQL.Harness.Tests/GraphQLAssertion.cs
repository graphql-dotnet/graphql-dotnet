using Alba;
using Microsoft.AspNetCore.Http;
using GraphQL.SystemTextJson;

namespace GraphQL.Harness.Tests
{
    public abstract class GraphQLAssertion : IScenarioAssertion
    {
        public abstract void Assert(Scenario scenario, HttpContext context, ScenarioAssertionException ex);

        protected ExecutionResult CreateQueryResult(string result, ExecutionErrors errors = null)
            => result.ToExecutionResult(errors);
    }
}
