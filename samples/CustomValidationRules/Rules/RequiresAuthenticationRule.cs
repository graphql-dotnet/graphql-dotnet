using System.Security.Claims;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace CustomValidationRules.Rules;

/// <summary>
/// A custom validation rule that restricts access to specific fields
/// based on the user's authentication status. Fields can be marked as
/// requiring authentication by adding metadata to the field definition.
/// <para>
/// This rule demonstrates:
/// <list type="bullet">
///   <item>Using <see cref="ValidationRuleBase.GetPreNodeVisitorAsync"/> to inspect fields before argument parsing</item>
///   <item>Using <see cref="ValidationContext.User"/> to access the authenticated user</item>
///   <item>Using <see cref="MatchingNodeVisitor{TNode}"/> for clean node matching</item>
///   <item>Using field metadata to drive validation logic</item>
/// </list>
/// </para>
/// </summary>
/// <remarks>
/// This is a <b>pre-node visitor</b> rule. It runs during the first pass of validation,
/// before arguments have been parsed. This is ideal for access-control checks because
/// they don't need to inspect argument values — only the user's identity.
/// </remarks>
public class RequiresAuthenticationRule : ValidationRuleBase
{
    /// <summary>
    /// The metadata key used on <see cref="FieldType"/> to indicate that a field requires authentication.
    /// Set via <c>Field&lt;...&gt;("name").WithMetadata("RequiresAuth", true)</c> or
    /// <c>fieldType.WithMetadata("RequiresAuth", true)</c>.
    /// </summary>
    public const string REQUIRES_AUTH_METADATA_KEY = "RequiresAuth";

    /// <summary>
    /// Returns a static visitor instance that checks each field for authentication requirements.
    /// </summary>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(_visitor);

    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new(
        enter: (fieldNode, context) =>
        {
            // Get the field definition from the schema using TypeInfo
            var fieldDef = context.TypeInfo.GetFieldDef();

            if (fieldDef == null)
                return;

            // Check if this field requires authentication (via metadata)
            if (fieldDef.GetMetadata<bool>(REQUIRES_AUTH_METADATA_KEY))
            {
                // Check if the user is authenticated via the ValidationContext.User property
                var user = context.User;
                bool isAuthenticated = user?.Identity?.IsAuthenticated == true;

                if (!isAuthenticated)
                {
                    // Report a validation error — this stops query execution
                    context.ReportError(new ValidationError(
                        context.Document.Source,
                        "AUTH_REQUIRED",
                        $"Field '{fieldDef.Name}' requires authentication. Please authenticate and try again.",
                        fieldNode));
                }
            }
        });
}

/// <summary>
/// A more advanced authentication rule that supports role-based access control.
/// Fields can specify required roles via metadata.
/// </summary>
public class RoleBasedAccessRule : ValidationRuleBase
{
    /// <summary>
    /// The metadata key for specifying required roles as a comma-separated string.
    /// </summary>
    public const string REQUIRED_ROLES_METADATA_KEY = "RequiredRoles";

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(_visitor);

    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new(
        enter: (fieldNode, context) =>
        {
            var fieldDef = context.TypeInfo.GetFieldDef();
            if (fieldDef == null)
                return;

            var requiredRoles = fieldDef.GetMetadata<string>(REQUIRED_ROLES_METADATA_KEY);
            if (requiredRoles == null)
                return;

            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.ReportError(new ValidationError(
                    context.Document.Source,
                    "AUTH_REQUIRED",
                    $"Field '{fieldDef.Name}' requires authentication.",
                    fieldNode));
                return;
            }

            var roles = requiredRoles.Split(',').Select(r => r.Trim());
            var userRoles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet();

            if (!roles.Any(role => userRoles.Contains(role)))
            {
                context.ReportError(new ValidationError(
                    context.Document.Source,
                    "INSUFFICIENT_ROLE",
                    $"Access denied for field '{fieldDef.Name}'. Required roles: {requiredRoles}.",
                    fieldNode));
            }
        });
}
