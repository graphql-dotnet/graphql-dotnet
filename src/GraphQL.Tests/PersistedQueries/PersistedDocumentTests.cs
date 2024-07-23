using GraphQL.DI;
using GraphQL.PersistedDocuments;
using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.PersistedQueries;

public class PersistedDocumentTests
{
    private class Query
    {
        public static string? Test(string? arg) => arg;
    }

    private class Loader : IPersistedDocumentLoader
    {
        public ValueTask<string?> GetQueryAsync(string? documentIdPrefix, string documentIdPayload, CancellationToken cancellationToken)
        {
            var arg = $"{documentIdPrefix ?? "{null}"}:{documentIdPayload}";
            return new($$"""
                {
                  test(arg:"{{arg}}")
                }
                """);
        }
    }

    private static ValueTask<string?> GetQueryDelegate(ExecutionOptions options, string? documentIdPrefix, string documentIdPayload)
    {
        var arg = $"{documentIdPrefix ?? "{null}"}:{documentIdPayload}";
        return new($$"""
                {
                  test(arg:"{{arg}}")
                }
                """);
    }

    [Theory]
    [InlineData(1, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}""", """{"data":{"test":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}}""")]
    [InlineData(2, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcde"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(3, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(4, """{"documentId":"sha256:0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(5, """{"documentId":"md5:0123456789abcdef"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(6, """{"documentId":"test"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(7, """{"query":"{test(arg:\"abc\")}"}""", """{"errors":[{"message":"The request must have a documentId parameter.","extensions":{"code":"DOCUMENT_ID_MISSING","codes":["DOCUMENT_ID_MISSING"]}}]}""")]
    [InlineData(8, """{}""", """{"errors":[{"message":"The request must have a documentId parameter.","extensions":{"code":"DOCUMENT_ID_MISSING","codes":["DOCUMENT_ID_MISSING"]}}]}""")]
    [InlineData(9, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef","query":"{test}"}""", """{"errors":[{"message":"The request must not have both query and documentId parameters.","extensions":{"code":"INVALID_REQUEST","codes":["INVALID_REQUEST"]}}]}""")]
    public async Task Default_Loader_Tests(int num, string query, string expectedResponse)
    {
        _ = num;
        var response = await ExecuteRequestAsync(
            b => b.UsePersistedDocuments<Loader>(),
            query);
        response.ShouldBeCrossPlatJson(expectedResponse);
    }

    [Theory]
    [InlineData(1, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}""", """{"data":{"test":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}}""")]
    [InlineData(2, """{"documentId":"md5:0123456789abcdef"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(3, """{"documentId":"test"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(4, """{"query":"{test(arg:\"abc\")}"}""", """{"data":{"test":"abc"}}""")]
    [InlineData(5, """{}""", """{"errors":[{"message":"The request must have a documentId parameter.","extensions":{"code":"DOCUMENT_ID_MISSING","codes":["DOCUMENT_ID_MISSING"]}}]}""")]
    [InlineData(6, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef","query":"{test}"}""", """{"errors":[{"message":"The request must not have both query and documentId parameters.","extensions":{"code":"INVALID_REQUEST","codes":["INVALID_REQUEST"]}}]}""")]
    public async Task Any_Request_Loader_Tests(int num, string query, string expectedResponse)
    {
        _ = num;
        var response = await ExecuteRequestAsync(
            b => b.UsePersistedDocuments<Loader>(GraphQL.DI.ServiceLifetime.Singleton, o => o.AllowOnlyPersistedDocuments = false),
            query);
        response.ShouldBeCrossPlatJson(expectedResponse);
    }

    [Theory]
    [InlineData(1, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}""", """{"data":{"test":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}}""")]
    [InlineData(2, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcde"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(3, """{"documentId":"md5:0123456789abcdef"}""", """{"data":{"test":"md5:0123456789abcdef"}}""")]
    [InlineData(4, """{"documentId":"test"}""", """{"data":{"test":"{null}:test"}}""")]
    [InlineData(5, """{"query":"{test(arg:\"abc\")}"}""", """{"errors":[{"message":"The request must have a documentId parameter.","extensions":{"code":"DOCUMENT_ID_MISSING","codes":["DOCUMENT_ID_MISSING"]}}]}""")]
    [InlineData(6, """{}""", """{"errors":[{"message":"The request must have a documentId parameter.","extensions":{"code":"DOCUMENT_ID_MISSING","codes":["DOCUMENT_ID_MISSING"]}}]}""")]
    [InlineData(7, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef","query":"{test}"}""", """{"errors":[{"message":"The request must not have both query and documentId parameters.","extensions":{"code":"INVALID_REQUEST","codes":["INVALID_REQUEST"]}}]}""")]
    public async Task Any_Prefix_Loader_Tests(int num, string query, string expectedResponse)
    {
        _ = num;
        var response = await ExecuteRequestAsync(
            b => b.UsePersistedDocuments<Loader>(GraphQL.DI.ServiceLifetime.Singleton, c => c.AllowedPrefixes.Clear()),
            query);
        response.ShouldBeCrossPlatJson(expectedResponse);
    }

    [Theory]
    [InlineData(1, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}""", """{"data":{"test":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}}""")]
    [InlineData(2, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcde"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(3, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(4, """{"documentId":"sha256:0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(5, """{"documentId":"md5:0123456789abcdef"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(6, """{"documentId":"test"}""", """{"errors":[{"message":"The format of the documentId parameter is invalid.","extensions":{"code":"DOCUMENT_ID_INVALID","codes":["DOCUMENT_ID_INVALID"]}}]}""")]
    [InlineData(7, """{"query":"{test(arg:\"abc\")}"}""", """{"errors":[{"message":"The request must have a documentId parameter.","extensions":{"code":"DOCUMENT_ID_MISSING","codes":["DOCUMENT_ID_MISSING"]}}]}""")]
    [InlineData(8, """{}""", """{"errors":[{"message":"The request must have a documentId parameter.","extensions":{"code":"DOCUMENT_ID_MISSING","codes":["DOCUMENT_ID_MISSING"]}}]}""")]
    [InlineData(9, """{"documentId":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef","query":"{test}"}""", """{"errors":[{"message":"The request must not have both query and documentId parameters.","extensions":{"code":"INVALID_REQUEST","codes":["INVALID_REQUEST"]}}]}""")]
    public async Task Default_Options_Tests(int num, string query, string expectedResponse)
    {
        _ = num;
        var response = await ExecuteRequestAsync(
            b => b.UsePersistedDocuments(c => c.GetQueryDelegate = GetQueryDelegate),
            query);
        response.ShouldBeCrossPlatJson(expectedResponse);
    }

    private async Task<string> ExecuteRequestAsync(Action<IGraphQLBuilder> builder, string requestJson)
    {
        using var provider = new ServiceCollection()
            .AddGraphQL(b =>
            {
                b.AddAutoSchema<Query>();
                b.AddSystemTextJson();
                builder(b);
            })
            .BuildServiceProvider();
        var executer = provider.GetRequiredService<IDocumentExecuter<ISchema>>();
        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
        var request = serializer.Deserialize<GraphQLRequest>(requestJson)!;
        var response = await executer.ExecuteAsync(o =>
        {
            o.Query = request.Query;
            o.DocumentId = request.DocumentId;
            o.Variables = request.Variables;
            o.Extensions = request.Extensions;
            o.RequestServices = provider;
        });
        return serializer.Serialize(response);
    }
}
