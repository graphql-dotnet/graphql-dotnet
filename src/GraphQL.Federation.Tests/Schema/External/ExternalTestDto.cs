using GraphQL.Federation.Attributes;

namespace GraphQL.Federation.Tests.Schema.External;

[Key("id", Resolvable = false)]
public class ExternalTestDto
{
    public int Id { get; set; }
}
