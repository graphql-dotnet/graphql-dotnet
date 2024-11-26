using GraphQL.Federation.CodeFirst.Sample3;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GraphQL.Federation.Tests;

public class Sample3Tests
{
    [Fact]
    public async Task Schema()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var ret = await webApp.Server.ExecuteGraphQLRequest("/graphql", "{ _service { sdl } }");
        ret.GetSdl().ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task Entities()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var query = "query ($arg: [_Any!]!) { _entities(representations: $arg) { __typename ... on Product { id reviews { ...reviewFragment } } ... on User { id reviews { ...reviewFragment } } } } fragment reviewFragment on Review { id content product { id } author { id } }";
        var variables = """{ "arg": [{ "__typename": "Product", "id": "1" },{ "__typename": "User", "id": "1" }] }""";
        var cat = await webApp.Server.ExecuteGraphQLRequest("/graphql", query, variables);
        cat.ShouldMatchApproved(o => o.NoDiff());
    }
}
