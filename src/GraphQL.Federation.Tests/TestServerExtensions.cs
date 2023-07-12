using System.Net;
using System.Text;
using System.Text.Json;
using GraphQL.SystemTextJson;
using Microsoft.AspNetCore.TestHost;

namespace GraphQL.Federation.Tests;

public static class TestServerExtensions
{
    private static readonly GraphQLSerializer _serializer = new();
    private static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    public static async Task<string> ExecuteGraphQLRequest(this TestServer server, string url, string query, object? variables = null)
    {
        // create url
        var uriBuilder = new UriBuilder(server.BaseAddress)
        {
            Path = url
        };
        url = uriBuilder.Uri.ToString();

        // create request
        var request = new
        {
            query,
            variables = variables switch
            {
                null => (object?)null,
                // interpret a string as a json object
                string str => JsonSerializer.Deserialize<JsonElement>(str),
                _ => variables,
            },
        };
        string json = _serializer.Serialize(request); // support Inputs for variables
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // create client and send request
        using var client = server.CreateClient();
        using var response = await client
            .PostAsync(url, content)
            .ConfigureAwait(false);

        // validate response
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"GraphQL request failed with status code {response.StatusCode}.");
        }
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        // reformat response as an indented json string
        var ret = await JsonSerializer.DeserializeAsync<JsonElement>(stream).ConfigureAwait(false);
        return JsonSerializer.Serialize(ret, _serializerOptions);
    }
}
