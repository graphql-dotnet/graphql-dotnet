using System.Text.Json;
using GraphQL.DataLoader;
using GraphQL.Federation.Tests.Fixtures;
using GraphQL.SystemTextJson;

namespace GraphQL.Federation.Tests;

// dotnet test GraphQL.Federation.Tests --filter DisplayName~GraphQL.Federation.Tests.SchemaFirstFederationTest.ServiceTest
// dotnet test GraphQL.Federation.Tests --filter DisplayName~GraphQL.Federation.Tests.SchemaFirstFederationTest.EntitiesTest
[Collection(nameof(SchemaFirstCollectionDefinition))]
public class SchemaFirstFederationTest : BaseSchemaFirstGraphQLTest
{
    public SchemaFirstFederationTest(SchemaFirstFixture schemaFirstFixture)
        : base(schemaFirstFixture)
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
            .GetString();
        sdl.ShouldBe("""
            type Query {
              _noop: String
            }

            type SchemaFirstExternalResolvableTestDto @key(fields: "id") {
              id: Int!
              external: String @external
              extended: String! @requires(fields: "external")
            }

            type SchemaFirstExternalTestDto @key(fields: "id", resolvable: false) {
              id: Int!
            }

            type SchemaFirstFederatedTestDto @key(fields: "id") {
              id: Int!
              name: String @deprecated(reason: "Test deprecation reason 03.")
              externalTestId: Int!
              externalResolvableTestId: Int!
              externalTest: SchemaFirstExternalTestDto! @deprecated(reason: "Test deprecation reason 04.")
              externalResolvableTest: SchemaFirstExternalResolvableTestDto! @provides(fields: "external")
            }


            extend schema @link(url: "https://specs.apollo.dev/federation/v2.0", import: ["@key", "@shareable", "@inaccessible", "@override", "@external", "@provides", "@requires"])
            """,
            StringCompareShould.IgnoreLineEndings);
    }

    [Fact]
    public async Task EntitiesTest()
    {
        // WaitForDebugger();

        var json = await Schema.ExecuteAsync(new GraphQLSerializer(), options =>
        {
            options.RequestServices = Services;
            options.Listeners.Add(new DataLoaderDocumentListener(new DataLoaderContextAccessor()));
            options.ThrowOnUnhandledException = true;
            options.Query = """
                query {
                    _entities(representations: [{ __typename: "SchemaFirstFederatedTestDto", id: 1 }, { __typename: "SchemaFirstFederatedTestDto", id: 3 }, { __typename: "SchemaFirstExternalResolvableTestDto", id: 321, external: "zxcvbn" }]) {
                        ... on SchemaFirstFederatedTestDto {
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
                        ... on SchemaFirstExternalResolvableTestDto {
                            id
                            extended
                        }
                    }
                }
                """;
        });

        json.ShouldBe(
            """{"data":{"_entities":[{"name":"111","externalTest":{"id":4},"externalResolvableTest":{"id":7,"external":"qwerty","extended":"qwerty"}},{"name":"333","externalTest":{"id":6},"externalResolvableTest":{"id":9,"external":"qwerty","extended":"qwerty"}},{"id":321,"extended":"zxcvbn"}]}}""",
            StringCompareShould.IgnoreLineEndings);
    }
}
