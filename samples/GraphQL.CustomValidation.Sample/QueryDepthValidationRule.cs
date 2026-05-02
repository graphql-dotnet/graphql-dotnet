using GraphQL.Validation;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.CustomValidation.Sample;

/// <summary>
/// Custom validation rule that limits the maximum depth of a GraphQL query.
/// This helps prevent denial-of-service attacks from deeply nested queries.
/// </summary>
public class QueryDepthValidationRule : ValidationRuleBase
{
    private readonly int _maxDepth;

    public QueryDepthValidationRule(int maxDepth)
    {
        _maxDepth = maxDepth;
    }

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new DepthVisitor(_maxDepth));

    private class DepthVisitor : INodeVisitor
    {
        private readonly int _maxDepth;

        public DepthVisitor(int maxDepth)
        {
            _maxDepth = maxDepth;
        }

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField field)
            {
                int depth = CalculateDepth(field);
                if (depth > _maxDepth)
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        $"QUERY_DEPTH_EXCEEDED",
                        $"Query depth of {depth} exceeds the maximum allowed depth of {_maxDepth}.",
                        field));
                }
            }
            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
            => default;

        private static int CalculateDepth(GraphQLField field, int currentDepth = 1)
        {
            if (field.SelectionSet == null || field.SelectionSet.Selections.Count == 0)
                return currentDepth;

            int maxChildDepth = currentDepth;
            foreach (var selection in field.SelectionSet.Selections)
            {
                if (selection is GraphQLField childField)
                {
                    int childDepth = CalculateDepth(childField, currentDepth + 1);
                    maxChildDepth = Math.Max(maxChildDepth, childDepth);
                }
            }
            return maxChildDepth;
        }
    }
}
