using System.Net;
using System.Text;
using System.Text.Json;
using GraphQL.SystemTextJson;
using GraphQL.Transport;
using Microsoft.AspNetCore.TestHost;

namespace GraphQL.Federation.Tests;

public static class TestServerExtensions
{
    private static readonly GraphQLSerializer _serializer = new();
    private static readonly JsonSerializerOptions _formattedOptions = new() { WriteIndented = true };

    public static Task<string> ExecuteGraphQLRequest(this TestServer server, string url, string query, object? variables = null)
    {
        var request = new GraphQLRequest
        {
            Query = query,
            Variables = GetVariables(variables),
        };
        return server.ExecuteGraphQLRequest(url, request);

        static Inputs? GetVariables(object? variables)
        {
            if (variables is null)
                return null;

            if (variables is string str)
                return _serializer.Deserialize<Inputs>(str);

            // convert anonymous object to Inputs dictionary
            return _serializer.Deserialize<Inputs>(_serializer.Serialize(variables));
        }
    }

    public static async Task<string> ExecuteGraphQLRequest(this TestServer server, string url, GraphQLRequest request)
    {
        var uriBuilder = new UriBuilder(server.BaseAddress)
        {
            Path = url
        };
        url = uriBuilder.Uri.ToString();

        string json = _serializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var client = server.CreateClient();
        using var response = await client
            .PostAsync(url, content)
            .ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"GraphQL request failed with status code {response.StatusCode}.");
        }
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var ret = await JsonSerializer.DeserializeAsync<JsonElement>(stream).ConfigureAwait(false);
        return JsonSerializer.Serialize(ret, _formattedOptions);
    }
}
