using GraphQL.Federation.TypeFirst.Sample4;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GraphQL.Federation.Tests;

public class Sample4Tests
{
    [Fact]
    public async Task Schema()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var ret = await webApp.Server.ExecuteGraphQLRequest("/graphql", "{ _service { sdl } }");
        ret.GetSdl().ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task Users()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var ret = await webApp.Server.ExecuteGraphQLRequest("/graphql", "{ users { id username } }");
        ret.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task Entities()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var query = "query ($arg: [_Any!]!) { _entities(representations: $arg) { __typename ... on User { id username } } }";
        var variables = """{ "arg": [{ "__typename": "User", "id": "1" },{ "__typename": "User", "id": "2" }] }""";
        var cat = await webApp.Server.ExecuteGraphQLRequest("/graphql", query, variables);
        cat.ShouldMatchApproved(o => o.NoDiff());
    }
}
