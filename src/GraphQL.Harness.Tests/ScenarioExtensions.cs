using Alba;

namespace GraphQL.Harness.Tests;

public static class ScenarioExtensions
{
    public static GraphQLExpectations GraphQL(this Scenario scenario) => new GraphQLExpectations(scenario);
}
