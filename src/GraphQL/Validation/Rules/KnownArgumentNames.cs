using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Known argument names:
///
/// A GraphQL field is only valid if all supplied arguments are defined by
/// that field.
/// </summary>
public class KnownArgumentNames : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly KnownArgumentNames Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="KnownArgumentNames"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public KnownArgumentNames()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="KnownArgumentNamesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLArgument>((node, context) =>
    {
        var argumentOf = context.TypeInfo.GetAncestor(2);
        if (argumentOf is GraphQLField)
        {
            var fieldDef = context.TypeInfo.GetFieldDef();
            if (fieldDef != null)
            {
                var fieldArgDef = fieldDef.Arguments?.Find(node.Name);
                if (fieldArgDef == null)
                {
                    var parentType = context.TypeInfo.GetParentType() ?? throw new InvalidOperationException("Parent type must not be null.");
                    context.ReportError(new KnownArgumentNamesError(context, node, fieldDef, parentType));
                }
            }
        }
        else if (argumentOf is GraphQLDirective)
        {
            var directive = context.TypeInfo.GetDirective();
            if (directive != null)
            {
                var directiveArgDef = directive.Arguments?.Find(node.Name);
                if (directiveArgDef == null)
                {
                    context.ReportError(new KnownArgumentNamesError(context, node, directive));
                }
            }
        }
    });
}
