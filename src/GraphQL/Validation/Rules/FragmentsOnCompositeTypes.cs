using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Fragments on composite type:
///
/// Fragments use a type condition to determine if they apply, since fragments
/// can only be spread into a composite type (object, interface, or union), the
/// type condition must also be a composite type.
/// </summary>
public class FragmentsOnCompositeTypes : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly FragmentsOnCompositeTypes Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="FragmentsOnCompositeTypes"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public FragmentsOnCompositeTypes()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="FragmentsOnCompositeTypesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
        new MatchingNodeVisitor<GraphQLInlineFragment>((node, context) =>
        {
            var type = context.TypeInfo.GetLastType();
            if (node.TypeCondition?.Type != null && type != null && !type.IsCompositeType())
            {
                context.ReportError(new FragmentsOnCompositeTypesError(context, node));
            }
        }),

        new MatchingNodeVisitor<GraphQLFragmentDefinition>((node, context) =>
        {
            var type = context.TypeInfo.GetLastType();
            if (type != null && !type.IsCompositeType())
            {
                context.ReportError(new FragmentsOnCompositeTypesError(context, node));
            }
        })
    );
}
