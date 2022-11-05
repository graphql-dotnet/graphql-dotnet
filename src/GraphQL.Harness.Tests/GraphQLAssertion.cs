using Alba;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Harness.Tests;

public abstract class GraphQLAssertion : IScenarioAssertion
{
    public abstract void Assert(Scenario scenario, HttpContext context, ScenarioAssertionException ex);

    protected ExecutionResult CreateQueryResult(string result, ExecutionErrors errors = null, bool executed = true)
        => result.ToExecutionResult(errors, executed);
}
