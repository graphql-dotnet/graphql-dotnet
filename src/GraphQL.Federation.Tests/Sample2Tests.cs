using GraphQL.Federation.Sample2;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GraphQL.Federation.Tests;

public class Sample2Tests
{
    [Fact]
    public async Task Schema()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var ret = await webApp.Server.ExecuteGraphQLRequest("/graphql", "{ _service { sdl } }").ConfigureAwait(false);
        ret.GetSdl().ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task Products()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var ret = await webApp.Server.ExecuteGraphQLRequest("/graphql", "{ products { id name category { id } } }").ConfigureAwait(false);
        ret.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task Entities()
    {
        await using var webApp = new WebApplicationFactory<Program>();
        var query = "query ($arg: [_Any!]!) { _entities(representations: $arg) { __typename ... on Category { id products { id name } } ... on Product { id name category { id } } } }";
        var variables = """{ "arg": [{ "__typename": "Category", "id": "1" },{ "__typename": "Product", "id": "1" }] }""";
        var cat = await webApp.Server.ExecuteGraphQLRequest("/graphql", query, variables).ConfigureAwait(false);
        cat.ShouldMatchApproved(o => o.NoDiff());
    }
}
