using GraphQL.Federation.Attributes;

namespace GraphQL.Federation.Tests.Schema.Output;

[Key(nameof(Id))]
public class FederatedTestDto
{
    public int Id { get; set; }
    [GraphQLMetadata(DeprecationReason = "Test deprecation reason 01.")]
    public string Name { get; set; }

    public int ExternalTestId { get; set; }
    public int ExternalResolvableTestId { get; set; }
}
