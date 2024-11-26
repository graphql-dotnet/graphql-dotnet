using Alba;
using GraphQL.SystemTextJson;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Harness.Tests;

public class SuccessResultAssertion : GraphQLAssertion
{
    private static readonly string extensionsKey = nameof(ExecutionResult.Extensions).ToLower();
    private readonly string _result;
    private readonly bool _ignoreExtensions;
    private readonly IGraphQLTextSerializer _writer = new GraphQLSerializer();

    public SuccessResultAssertion(string result, bool ignoreExtensions)
    {
        _result = result;
        _ignoreExtensions = ignoreExtensions;
    }

    public override void Assert(Scenario scenario, HttpContext context, ScenarioAssertionException ex)
    {
        var expectedResult = CreateQueryResult(_result);

        // for Alba v4
        // string actualResultJson = ex.ReadBody(context);

        // for Alba v6 [ScenarioAssertionException.ReadBody internal]
        context.Request.Body.Position = 0;
        string actualResultJson = new StreamReader(context.Response.Body).ReadToEnd();

        if (_ignoreExtensions)
        {
            expectedResult.Extensions = null;

            var actualResult = actualResultJson.ToDictionary();
            if (actualResult.ContainsKey(extensionsKey))
            {
                actualResult.Remove(extensionsKey);
            }
            actualResultJson = _writer.Serialize(actualResult);
        }

        string expectedResultJson = _writer.Serialize(expectedResult);

        if (!actualResultJson.Equals(expectedResultJson))
        {
            ex.Add($"Expected '{expectedResultJson}' but got '{actualResultJson}'");
        }
    }
}
