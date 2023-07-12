using GraphQL.Federation.Attributes;

namespace GraphQL.Federation.Tests.Schema.Output;

[Key(nameof(Id))]
[Shareable]
[Inaccessible]
public class DirectivesTestDto
{
    public int Id { get; set; }
    [Shareable]
    public string Shareable { get; set; }
    [Inaccessible]
    public string Inaccessible { get; set; }
    [Override("OtherSubgraph")]
    public string Override { get; set; }
    [External]
    public string External { get; set; }
    [Provides("Foo", "Bar")]
    public string Provides { get; set; }
    [Requires("foo bar")]
    public string Requires { get; set; }
}
