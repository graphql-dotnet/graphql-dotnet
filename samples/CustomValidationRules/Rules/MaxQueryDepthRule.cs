using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace CustomValidationRules.Rules;

/// <summary>
/// A custom validation rule that limits the maximum depth of a GraphQL query.
/// Deeply nested queries can be expensive and may be used for denial-of-service attacks.
/// <para>
/// This rule demonstrates:
/// <list type="bullet">
///   <item>Using <see cref="ValidationRuleBase.GetPreNodeVisitorAsync"/> for depth tracking during node traversal</item>
///   <item>Tracking state across enter/leave node visitor events</item>
///   <item>Using the <see cref="MatchingNodeVisitor{TNode,TState}"/> overload with custom state</item>
/// </list>
/// </para>
/// </summary>
/// <remarks>
/// This is a <b>pre-node visitor</b> rule. It counts nesting depth as the AST is traversed
/// and reports an error if the configured maximum depth is exceeded.
/// <para>
/// Note: For production use, consider using the built-in <c>ComplexityValidationRule</c>
/// which provides a more comprehensive analysis including both depth and complexity scoring.
/// This example is provided to demonstrate how to build a custom rule that tracks state.
/// </para>
/// </remarks>
public class MaxQueryDepthRule : ValidationRuleBase
{
    private readonly int _maxDepth;

    /// <summary>
    /// Creates a new instance of <see cref="MaxQueryDepthRule"/> with the specified maximum allowed depth.
    /// </summary>
    /// <param name="maxDepth">The maximum allowed nesting depth for queries.</param>
    public MaxQueryDepthRule(int maxDepth)
    {
        _maxDepth = maxDepth;
    }

    /// <summary>
    /// Returns a node visitor that tracks query depth during traversal.
    /// Uses a stateful visitor to increment/decrement depth as we enter/leave field nodes.
    /// </summary>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new DepthTrackingVisitor(_maxDepth));

    /// <summary>
    /// A stateful node visitor that tracks the current depth of the query
    /// by counting field nesting levels.
    /// </summary>
    private class DepthTrackingVisitor : INodeVisitor
    {
        private readonly int _maxDepth;
        private int _currentDepth;
        private int _maxObservedDepth;

        public DepthTrackingVisitor(int maxDepth)
        {
            _maxDepth = maxDepth;
        }

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            // Track depth for field selections within selection sets
            if (node is GraphQLField)
            {
                _currentDepth++;
                _maxObservedDepth = Math.Max(_maxObservedDepth, _currentDepth);

                if (_currentDepth > _maxDepth)
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "MAX_QUERY_DEPTH",
                        $"Query depth {_currentDepth} exceeds maximum allowed depth of {_maxDepth}.",
                        node));
                }
            }

            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField)
            {
                _currentDepth--;
            }

            return default;
        }
    }
}

/// <summary>
/// A validation rule that limits the total number of fields (nodes) in a query.
/// This helps prevent overly complex queries that could degrade server performance.
/// <para>
/// This rule demonstrates a <b>post-node visitor</b> — it runs after all arguments
/// have been parsed and validated. Post-node visitors are ideal for whole-document
/// analysis that doesn't need to stop early.
/// </para>
/// </summary>
public class MaxFieldCountRule : ValidationRuleBase
{
    private readonly int _maxFields;

    /// <summary>
    /// Creates a new instance with the specified maximum allowed field count.
    /// </summary>
    public MaxFieldCountRule(int maxFields)
    {
        _maxFields = maxFields;
    }

    /// <summary>
    /// Returns a post-node visitor that counts all fields in the document.
    /// Post-node visitors run after argument parsing, so <see cref="ValidationContext.ArgumentValues"/>
    /// and <see cref="ValidationContext.DirectiveValues"/> are available.
    /// </summary>
    public override ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context)
        => new(new FieldCountVisitor(_maxFields));

    private class FieldCountVisitor : INodeVisitor
    {
        private readonly int _maxFields;
        private int _fieldCount;

        public FieldCountVisitor(int maxFields)
        {
            _maxFields = maxFields;
        }

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField)
            {
                _fieldCount++;
                if (_fieldCount > _maxFields)
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "MAX_FIELD_COUNT",
                        $"Query contains more than {_maxFields} fields. " +
                        $"Please simplify your query.",
                        node));
                }
            }
            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
            => default;
    }
}
