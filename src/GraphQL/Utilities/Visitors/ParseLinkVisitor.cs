using GraphQL.Types;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Parses @link directives applied to the schema.
/// </summary>
public sealed class ParseLinkVisitor : BaseSchemaNodeVisitor
{
    private ParseLinkVisitor()
    {
    }

    /// <inheritdoc cref="ParseLinkVisitor"/>
    public static ParseLinkVisitor Instance { get; } = new();

    /// <inheritdoc/>
    public override void VisitSchema(ISchema schema)
    {
        var appliedDirectives = schema.GetAppliedDirectives();
        if (appliedDirectives == null)
            return;
        foreach (var appliedDirective in appliedDirectives)
        {
            LinkConfiguration.TryParseDirective(appliedDirective, true, out _);
        }
    }
}
