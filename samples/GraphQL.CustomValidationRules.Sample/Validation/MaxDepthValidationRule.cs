using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.CustomValidationRules.Sample.Validation;

/// <summary>
/// A custom validation rule that limits the maximum depth of a GraphQL query.
/// This helps prevent expensive deeply-nested queries that could overload the server.
///
/// This rule demonstrates how to extend <see cref="ValidationRuleBase"/> and use
/// <see cref="MatchingNodeVisitor{TNode}"/> to inspect AST nodes during validation.
/// </summary>
public class MaxDepthValidationRule : ValidationRuleBase
{
    private const int MAX_DEPTH = 5;

    /// <inheritdoc/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new MatchingNodeVisitor<GraphQLOperationDefinition>(
            enter: (op, ctx) =>
            {
                var depth = CalculateMaxDepth(op.SelectionSet);
                if (depth > MAX_DEPTH)
                {
                    ctx.ReportError(new ValidationError(
                        ctx.Document.Source,
                        "CUSTOM_DEPTH",
                        $"Query exceeds maximum allowed depth of {MAX_DEPTH} (actual depth: {depth}).",
                        op));
                }
            }));

    private static int CalculateMaxDepth(GraphQLSelectionSet? selectionSet, int currentDepth = 1)
    {
        if (selectionSet?.Selections == null || selectionSet.Selections.Count == 0)
            return currentDepth;

        int maxChildDepth = currentDepth;
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is GraphQLField field && field.SelectionSet != null)
            {
                var childDepth = CalculateMaxDepth(field.SelectionSet, currentDepth + 1);
                if (childDepth > maxChildDepth)
                    maxChildDepth = childDepth;
            }
        }

        return maxChildDepth;
    }
}
