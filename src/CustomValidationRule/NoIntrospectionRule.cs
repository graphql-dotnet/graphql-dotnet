using GraphQL.Validation;
using GraphQLParser.AST;

namespace CustomValidationRule;

/// <summary>
/// A custom validation rule that disallows introspection queries.
/// Any field whose name begins with "__" (such as __schema or __type) will be rejected.
/// </summary>
public class NoIntrospectionRule : IValidationRule
{
    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {
        return new ValueTask<INodeVisitor?>(
            new MatchingNodeVisitor<GraphQLField>(
                (field, ctx) =>
                {
                    if (field.Name.StringValue.StartsWith("__", StringComparison.Ordinal))
                    {
                        ctx.ReportError(new ValidationError(
                            ctx.Document.Source,
                            "NoIntrospection",
                            $"Introspection queries are not allowed. Field '{field.Name.StringValue}' is an introspection field.",
                            field));
                    }
                }));
    }
}
