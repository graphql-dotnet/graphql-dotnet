using GraphQL.Validation.Errors.Custom;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules.Custom;

/// <summary>
/// Analyzes the document for any introspection fields and reports an error if any are found.
/// </summary>
public class NoIntrospectionValidationRule : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly NoIntrospectionValidationRule Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="NoIntrospectionValidationRule"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public NoIntrospectionValidationRule()
    {
    }

    /// <inheritdoc/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_visitor);

    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new(
        (field, context) =>
        {
            if (field.Name.Value == "__schema" || field.Name.Value == "__type")
                context.ReportError(new NoIntrospectionError(context.Document.Source, field));
        });
}
