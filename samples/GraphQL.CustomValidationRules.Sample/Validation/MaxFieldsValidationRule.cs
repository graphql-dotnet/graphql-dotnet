using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.CustomValidationRules.Sample.Validation;

/// <summary>
/// A custom validation rule that limits the maximum number of fields in any
/// single selection set. This prevents overly broad queries that request too
/// many fields at once.
///
/// This rule demonstrates using <see cref="MatchingNodeVisitor{TNode}"/> with
/// the leave callback to inspect nodes after their children have been processed.
/// </summary>
public class MaxFieldsValidationRule : ValidationRuleBase
{
    private const int MAX_FIELDS = 20;

    /// <inheritdoc/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new MatchingNodeVisitor<GraphQLSelectionSet>(
            enter: (selectionSet, ctx) =>
            {
                int fieldCount = selectionSet.Selections
                    .Count(s => s is GraphQLField);

                if (fieldCount > MAX_FIELDS)
                {
                    ctx.ReportError(new ValidationError(
                        ctx.Document.Source,
                        "CUSTOM_MAX_FIELDS",
                        $"Selection set contains {fieldCount} fields, exceeding the maximum allowed {MAX_FIELDS} fields.",
                        selectionSet));
                }
            }));
}
