using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Lone anonymous operation:
///
/// A GraphQL document is only valid if when it contains an anonymous operation
/// (the query short-hand) that it contains only that one operation definition.
/// </summary>
public sealed class LoneAnonymousOperation : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
    public static readonly LoneAnonymousOperation Instance = new();
    private LoneAnonymousOperation() { }

    /// <inheritdoc/>
    /// <exception cref="LoneAnonymousOperationError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLOperationDefinition>((op, context) =>
    {
        if (op.Name is null && context.Document.OperationsCount() > 1)
        {
            context.ReportError(new LoneAnonymousOperationError(context, op));
        }
    });
}
