using GraphQL.Federation.Attributes;

namespace GraphQL.Federation.Tests.Schema.External;

[Key(nameof(Id), Resolvable = false)]
public class ExternalTestDto
{
    public int Id { get; set; }
}
