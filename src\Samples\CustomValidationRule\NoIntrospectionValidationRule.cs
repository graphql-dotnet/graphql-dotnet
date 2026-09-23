using GraphQL.Validation;
using GraphQLParser.AST;

namespace CustomValidationRuleSample;

/// <summary>
/// A custom validation rule that prohibits introspection queries.
/// This is useful in production environments where you want to
/// prevent clients from discovering your schema structure.
/// </summary>
/// <remarks>
/// GraphQL introspection fields have names that begin with double underscores (<c>__</c>),
/// such as <c>__schema</c>, <c>__type</c>, and <c>__typename</c>.
/// </remarks>
public class NoIntrospectionValidationRule : IValidationRule
{
    /// <inheritdoc/>
    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
        => new(Visitor.Instance);

    private sealed class Visitor : INodeVisitor
    {
        /// <summary>Singleton instance – the visitor is stateless so it can be safely reused.</summary>
        public static readonly Visitor Instance = new();

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField field)
            {
                var fieldName = field.Name.StringValue;
                if (fieldName.StartsWith("__", StringComparison.Ordinal))
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "NoIntrospection",
                        $"Introspection is disabled. Field '{fieldName}' is not allowed.",
                        field));
                }
            }

            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
            => default;
    }
}
