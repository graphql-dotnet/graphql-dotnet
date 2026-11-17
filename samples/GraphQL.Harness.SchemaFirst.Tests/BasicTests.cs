using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Harness.SchemaFirst.Tests;

public class BasicTests
{
    [Fact]
    public void VerifyIntrospection()
    {
        using var factory = new WebApplicationFactory<Program>();
        var schema = factory.Services.GetRequiredService<ISchema>();
        schema.Initialize();
        var sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });

        sdl.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task AllFields()
    {
        using var stream = typeof(BasicTests).Assembly.GetManifestResourceStream("GraphQL.Harness.SchemaFirst.Tests.AllFields.graphql")!;
        using var reader = new StreamReader(stream);
        string query = await reader.ReadToEndAsync();

        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var results = new JsonObject();

        foreach (string operationName in new[] { "AllFields", "CreateHuman" })
        {
            using var response = await client.PostAsJsonAsync("/graphql", new { query, operationName });
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            string json = await response.Content.ReadAsStringAsync();
            var result = JsonNode.Parse(json);
            result.ShouldNotBeNull();
            result["errors"].ShouldBeNull(json);
            results[operationName] = result;
        }

        results.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ShouldMatchApproved(o => o.NoDiff());
    }
}
