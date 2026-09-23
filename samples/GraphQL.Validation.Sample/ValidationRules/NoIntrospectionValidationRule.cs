using GraphQL.Language.AST;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Sample.ValidationRules;

/// <summary>
/// This validation rule prevents introspection queries from being executed.
/// It is useful for production environments where you want to disable introspection
/// for security reasons.
/// </summary>
public class NoIntrospectionValidationRule : ValidationRuleBase, INodeVisitor
{
    /// <summary>
    /// Returns a node visitor that runs before argument parsing.
    /// This visitor checks for introspection fields in the query.
    /// </summary>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(this);

    /// <inheritdoc/>
    public ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
    {
        if (node is GraphQLField field)
        {
            // Check if the field is an introspection field
            if (field.Name.Value is "__schema" or "__type")
            {
                context.ReportError(new NoIntrospectionError(context.Document.Source, field));
            }
        }
        return default;
    }

    /// <inheritdoc/>
    public ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context) => default;
}

/// <summary>
/// Validation error for when an introspection query is detected.
/// </summary>
public class NoIntrospectionError : ValidationError
{
    /// <summary>
    /// Creates a new instance of the NoIntrospectionError.
    /// </summary>
    public NoIntrospectionError(ROM source, GraphQLField field)
        : base(source, null, "Introspection queries are not allowed", field)
    {
    }
}
