using GraphQL.Validation;
using GraphQLParser.AST;

namespace CustomValidationRuleSample;

/// <summary>
/// A custom validation rule that prohibits introspection queries.
/// This is useful in production environments where you want to
/// prevent clients from discovering your schema structure.
/// </summary>
/// <remarks>
/// Introspection fields are those whose names begin with double underscores (__),
/// such as __schema, __type, and __typename.
/// </remarks>
public class NoIntrospectionValidationRule : IValidationRule
{
    /// <inheritdoc/>
    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
        => new(Visitor.Instance);

    private sealed class Visitor : INodeVisitor
    {
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
