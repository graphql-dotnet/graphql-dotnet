using GraphQL.Language.AST;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser.AST;

namespace GraphQL.Validation.Sample.ValidationRules.QueryDepth;

/// <summary>
/// This validation rule limits the maximum depth of a GraphQL query to prevent
/// overly complex queries that could impact server performance.
/// </summary>
public class MaxQueryDepthValidationRule : ValidationRuleBase, INodeVisitor
{
    private readonly int _maxDepth;
    private int _currentDepth;

    /// <summary>
    /// Creates a new instance of MaxQueryDepthValidationRule with the specified maximum depth.
    /// </summary>
    /// <param name="maxDepth">The maximum allowed query depth.</param>
    public MaxQueryDepthValidationRule(int maxDepth = 5)
    {
        _maxDepth = maxDepth;
    }

    /// <summary>
    /// Returns a node visitor that runs before argument parsing.
    /// </summary>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
    {
        _currentDepth = 0;
        return new(this);
    }

    /// <summary>
    /// Called when entering a field node. Increments the depth counter and checks if it exceeds the maximum.
    /// </summary>
    public ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
    {
        if (node is GraphQLField field)
        {
            // Skip introspection fields
            if (field.Name.Value.StartsWith("__"))
                return default;

            _currentDepth++;

            if (_currentDepth > _maxDepth)
            {
                context.ReportError(new ValidationError(
                    context.Document.Source,
                    null,
                    $"Query exceeds maximum depth of {_maxDepth}",
                    field));
            }
        }
        else if (node is GraphQLSelectionSet)
        {
            // Note: This is a simplified implementation.
            // A production-ready implementation would track selections more carefully
            // to avoid false positives with inline fragments and fragment spreads.
        }

        return default;
    }

    /// <summary>
    /// Called when leaving a field node. Decrements the depth counter.
    /// </summary>
    public ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context)
    {
        if (node is GraphQLField field)
        {
            if (!field.Name.Value.StartsWith("__"))
            {
                _currentDepth--;
            }
        }

        return default;
    }
}
