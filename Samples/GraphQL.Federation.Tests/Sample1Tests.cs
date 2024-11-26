using GraphQL.Federation.SchemaFirst.Sample1;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GraphQL.Federation.Tests;

public class Sample1Tests
{
    [Fact]
    public async Task Schema()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var ret = await webApp.Server.ExecuteGraphQLRequest("/graphql", "{ _service { sdl } }");
        ret.GetSdl().ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task Categories()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var ret = await webApp.Server.ExecuteGraphQLRequest("/graphql", "{ categories { id name } }");
        ret.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task Entities()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var query = "query ($arg: [_Any!]!) { _entities(representations: $arg) { __typename ... on Category { id name } } }";
        var variables = """{ "arg": [{ "__typename": "Category", "id": "1" }] }""";
        var cat = await webApp.Server.ExecuteGraphQLRequest("/graphql", query, variables);
        cat.ShouldMatchApproved(o => o.NoDiff());
    }
}
