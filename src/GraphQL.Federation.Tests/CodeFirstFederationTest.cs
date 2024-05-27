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
            .GetString();

        sdl.ShouldBe("""
            schema @link(url: "https://specs.apollo.dev/federation/v2.0", import: ["@key", "@shareable", "@inaccessible", "@override", "@external", "@provides", "@requires"]) {
              query: TestQuery
            }

            directive @link(url: String!, as: String, for: link__Purpose, import: [link__Import]) repeatable on SCHEMA

            directive @key(fields: String!, resolvable: Boolean = true) repeatable on OBJECT | INTERFACE

            directive @shareable on FIELD_DEFINITION | OBJECT

            directive @inaccessible on FIELD_DEFINITION | INTERFACE | OBJECT | UNION | ARGUMENT_DEFINITION | SCALAR | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION

            directive @override(from: String!) on FIELD_DEFINITION

            directive @external on FIELD_DEFINITION | OBJECT

            directive @provides(fields: FieldSet!) on FIELD_DEFINITION

            directive @requires(fields: FieldSet!) on FIELD_DEFINITION

            scalar _Any

            type TestQuery {
              directivesTest: DirectivesTestDto!
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type DirectivesTestDto @key(fields: "id") @shareable @inaccessible {
              id: Int!
              shareable: String! @shareable
              inaccessible: String! @inaccessible
              override: String! @override(from: "OtherSubgraph")
              external: String! @external
              provides: String! @provides(fields: "foo bar")
              requires: String! @requires(fields: "foo bar")
            }

            type _Service {
              sdl: String
            }

            union _Entity = DirectivesTestDto | FederatedTestDto | ExternalResolvableTestDto

            type FederatedTestDto @key(fields: "id") {
              id: Int!
              name: String! @deprecated(reason: "Test deprecation reason 01.")
              externalTestId: Int!
              externalResolvableTestId: Int!
              externalTest: ExternalTestDto! @deprecated(reason: "Test deprecation reason 02.")
              externalResolvableTest: ExternalResolvableTestDto! @provides(fields: "external")
            }

            type ExternalTestDto @key(fields: "id", resolvable: false) {
              id: Int!
            }

            type ExternalResolvableTestDto @key(fields: "id") {
              id: Int!
              external: String! @external
              extended: String! @requires(fields: "External")
            }

            scalar link__Purpose

            scalar link__Import

            scalar FieldSet
            """,
            StringCompareShould.IgnoreLineEndings);
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
