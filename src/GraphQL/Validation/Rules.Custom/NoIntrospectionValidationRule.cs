using GraphQL.Validation.Errors.Custom;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules.Custom;

/// <summary>
/// Analyzes the document for any introspection fields and reports an error if any are found.
/// </summary>
public sealed class NoIntrospectionValidationRule : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
    public static readonly NoIntrospectionValidationRule Instance = new();
    private NoIntrospectionValidationRule() { }

    /// <inheritdoc/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_visitor);

    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new(
        (field, context) =>
        {
            if (field.Name.Value == "__schema" || field.Name.Value == "__type")
                context.ReportError(new NoIntrospectionError(context.Document.Source, field));
        });
}
