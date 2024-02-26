using GraphQL.Federation.Attributes;

namespace GraphQL.Federation.Tests.Schema.Output;

[Key("id")]
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
    [Provides("foo", "bar")]
    public string Provides { get; set; }
    [Requires("foo bar")]
    public string Requires { get; set; }
}
