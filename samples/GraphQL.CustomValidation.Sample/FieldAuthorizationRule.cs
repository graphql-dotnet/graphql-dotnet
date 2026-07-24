using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.CustomValidation.Sample;

/// <summary>
/// Custom validation rule that restricts access to specific fields
/// based on user authentication status.
/// </summary>
public class FieldAuthorizationRule : ValidationRuleBase
{
    private readonly HashSet<string> _restrictedFields;
    private readonly Func<string?> _getCurrentUser;

    public FieldAuthorizationRule(
        IEnumerable<string> restrictedFields,
        Func<string?> getCurrentUser)
    {
        _restrictedFields = new HashSet<string>(restrictedFields, StringComparer.OrdinalIgnoreCase);
        _getCurrentUser = getCurrentUser;
    }

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new FieldAuthVisitor(_restrictedFields, _getCurrentUser));

    private class FieldAuthVisitor : INodeVisitor
    {
        private readonly HashSet<string> _restrictedFields;
        private readonly Func<string?> _getCurrentUser;

        public FieldAuthVisitor(
            HashSet<string> restrictedFields,
            Func<string?> getCurrentUser)
        {
            _restrictedFields = restrictedFields;
            _getCurrentUser = getCurrentUser;
        }

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is GraphQLField field && _restrictedFields.Contains(field.Name.StringValue))
            {
                var user = _getCurrentUser();
                if (string.IsNullOrEmpty(user))
                {
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "FIELD_ACCESS_DENIED",
                        $"Access denied for field '{field.Name.StringValue}'. Authentication required.",
                        field));
                }
            }
            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
            => default;
    }
}
