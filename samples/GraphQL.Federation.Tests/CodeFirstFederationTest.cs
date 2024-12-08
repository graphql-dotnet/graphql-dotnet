using System.Text.Json;
using GraphQL.DataLoader;
using GraphQL.Federation.Tests.Fixtures;
using GraphQL.SystemTextJson;

namespace GraphQL.Federation.Tests;

// dotnet test GraphQL.Federation.Tests --filter DisplayName~GraphQL.Federation.Tests.CodeFirstFederationTest.ServiceTest
// dotnet test GraphQL.Federation.Tests --filter DisplayName~GraphQL.Federation.Tests.CodeFirstFederationTest.EntitiesTest
[Collection(nameof(CodeFirstCollectionDefinition))]
public class CodeFirstFederationTest : BaseCodeFirstGraphQLTest
{
    public CodeFirstFederationTest(CodeFirstFixture codeFirstFixture)
        : base(codeFirstFixture)
    { }

    [Fact]
    public async Task ServiceTest()
    {
        // WaitForDebugger();

        var json = await Schema.ExecuteAsync(new GraphQLSerializer(), options =>
        {
            options.Listeners.Add(new DataLoaderDocumentListener(new DataLoaderContextAccessor()));
            options.ThrowOnUnhandledException = true;
            options.Query = """
                query {
                    _service {
                        sdl
                    }
                }
                """;
        });

        var sdl = JsonDocument.Parse(json).RootElement
            .GetProperty("data")
            .GetProperty("_service")
            .GetProperty("sdl")
            .GetString()!;

        sdl.ShouldMatchApproved(c => c.NoDiff());
    }

    [Fact]
    public async Task EntitiesTest()
    {
        // WaitForDebugger();

        var json = await Schema.ExecuteAsync(new GraphQLSerializer(), options =>
        {
            options.Listeners.Add(new DataLoaderDocumentListener(new DataLoaderContextAccessor()));
            options.ThrowOnUnhandledException = true;
            options.Query = """
                query {
                    _entities(representations: [{ __typename: "FederatedTestDto", id: 1 }, { __typename: "FederatedTestDto", id: 3 }, { __typename: "ExternalResolvableTestDto", id: 123, external: "asdfgh" }]) {
                        ... on FederatedTestDto {
                            name
                            externalTest {
                                id
                            }
                            externalResolvableTest {
                                id
                                external
                                extended
                            }
                        }
                        ... on ExternalResolvableTestDto {
                            id
                            extended
                        }
                    }
                }
                """;
        });

        json.ShouldBe(
            """{"data":{"_entities":[{"name":"111","externalTest":{"id":4},"externalResolvableTest":{"id":7,"external":"external-7","extended":"external-7"}},{"name":"333","externalTest":{"id":6},"externalResolvableTest":{"id":9,"external":"external-9","extended":"external-9"}},{"id":123,"extended":"asdfgh"}]}}""",
            StringCompareShould.IgnoreLineEndings);
    }
}
