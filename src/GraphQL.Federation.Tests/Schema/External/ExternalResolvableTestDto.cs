using GraphQL.Federation.Attributes;

namespace GraphQL.Federation.Tests.Schema.External;

[Key(nameof(Id))]
public class ExternalResolvableTestDto
{
    public int Id { get; set; }

    [External]
    public string External { get; set; }
}
