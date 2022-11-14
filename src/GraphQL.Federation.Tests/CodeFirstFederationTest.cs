using System.Text.Json;
using GraphQL.DataLoader;
using GraphQL.Federation.Tests.Fixtures;
using GraphQL.SystemTextJson;
using GraphQL.Tests;

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
            options.Query = @"
query {
    _service {
        sdl
    }
}";
        }).ConfigureAwait(false);

        var sdl = JsonDocument.Parse(json).RootElement
            .GetProperty("data")
            .GetProperty("_service")
            .GetProperty("sdl")
            .GetString();

        sdl.ShouldBeCrossPlat("schema {\n  query: TestQuery\n}\n\ntype DirectivesTestDto @key(fields: \"id\") @shareable @inaccessible {\n  id: Int!\n  shareable: String @shareable\n  inaccessible: String @inaccessible\n  override: String @override(from: \"OtherSubgraph\")\n  external: String @external\n  provides: String @provides(fields: \"foo bar\")\n  requires: String @requires(fields: \"foo bar\")\n}\n\ntype ExternalResolvableTestDto @key(fields: \"id\") {\n  id: Int!\n  external: String @external\n  extended: String! @requires(fields: \"external\")\n}\n\ntype ExternalTestDto @key(fields: \"id\", resolvable: false) {\n  id: Int!\n}\n\ntype FederatedTestDto @key(fields: \"id\") {\n  id: Int!\n  name: String @deprecated(reason: \"Test deprecation reason 01.\")\n  externalTestId: Int!\n  externalResolvableTestId: Int!\n  externalTest: ExternalTestDto! @deprecated(reason: \"Test deprecation reason 02.\")\n  externalResolvableTest: ExternalResolvableTestDto! @provides(fields: \"external\")\n}\n\ntype TestQuery {\n  directivesTest: DirectivesTestDto!\n}\n\n\nextend schema @link(url: \"https://specs.apollo.dev/federation/v2.0\", import: [\"@key\", \"@shareable\", \"@inaccessible\", \"@override\", \"@external\", \"@provides\", \"@requires\"])");
    }

    [Fact]
    public async Task EntitiesTest()
    {
        // WaitForDebugger();

        var json = await Schema.ExecuteAsync(new GraphQLSerializer(), options =>
        {
            options.Listeners.Add(new DataLoaderDocumentListener(new DataLoaderContextAccessor()));
            options.ThrowOnUnhandledException = true;
            options.Query = @"
query {
    _entities(representations: [{ __typename: ""FederatedTestDto"", id: 1 }, { __typename: ""FederatedTestDto"", id: 3 }, { __typename: ""ExternalResolvableTestDto"", id: 123, external: ""asdfgh"" }]) {
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
}";
        }).ConfigureAwait(false);

        json.ShouldBeCrossPlatJson("{\"data\":{\"_entities\":[{\"name\":\"111\",\"externalTest\":{\"id\":4},\"externalResolvableTest\":{\"id\":7,\"external\":\"external-7\",\"extended\":\"external-7\"}},{\"name\":\"333\",\"externalTest\":{\"id\":6},\"externalResolvableTest\":{\"id\":9,\"external\":\"external-9\",\"extended\":\"external-9\"}},{\"id\":123,\"extended\":\"asdfgh\"}]}}");
    }
}
