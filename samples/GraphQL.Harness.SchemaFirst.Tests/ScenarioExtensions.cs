using Alba;

namespace GraphQL.Harness.SchemaFirst.Tests;

public static class ScenarioExtensions
{
    public static GraphQLExpectations GraphQL(this Scenario scenario) => new(scenario);
}
