namespace GraphQL.Federation.Tests.Schema.External;

public class SchemaFirstFederatedTestDto
{
    public int Id { get; set; }
    [GraphQLMetadata(DeprecationReason = "Test deprecation reason 03.")]
    public string Name { get; set; }

    public int ExternalTestId { get; set; }
    public int ExternalResolvableTestId { get; set; }

    public SchemaFirstExternalTestDto ExternalTest(IResolveFieldContext ctx) =>
        new()
        {
            Id = ((SchemaFirstFederatedTestDto)ctx.Source).ExternalTestId
        };

    public SchemaFirstExternalResolvableTestDto ExternalResolvableTest(IResolveFieldContext ctx) =>
        new()
        {
            Id = ((SchemaFirstFederatedTestDto)ctx.Source).ExternalResolvableTestId,
            External = "qwerty"
        };
}
