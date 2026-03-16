using GraphQL.Validation;
using GraphQLParser.AST;

/// <summary>
/// A custom validation rule that prohibits introspection queries.
/// This can be useful in production environments where you want to
/// hide the schema structure from clients.
/// </summary>
public class NoIntrospectionValidationRule : IValidationRule
{
    /// <inheritdoc/>
    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
        => new(new NoIntrospectionVisitor());

    private class NoIntrospectionVisitor : INodeVisitor
    {
        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField field)
            {
                var name = field.Name.StringValue;
                if (name.StartsWith("__"))
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "NoIntrospection",
                        $"Introspection queries are not allowed. Field '{name}' is an introspection field.",
                        field));
                }
            }

            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
            => default;
    }
}
