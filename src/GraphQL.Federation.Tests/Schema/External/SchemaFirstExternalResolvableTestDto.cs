namespace GraphQL.Federation.Tests.Schema.External;

public class SchemaFirstExternalResolvableTestDto
{
    public int Id { get; set; }

    public string External { get; set; }

    public string Extended(IResolveFieldContext ctx) =>
        ((SchemaFirstExternalResolvableTestDto)ctx.Source).External;
}
