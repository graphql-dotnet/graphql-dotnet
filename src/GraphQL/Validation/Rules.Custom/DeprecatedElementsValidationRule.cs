using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules.Custom;

/// <summary>
/// A GraphQL validation rule that identifies deprecated fields, arguments, and types referenced in the document
/// and calls abstract methods for each deprecated element found. This allows custom
/// handling of deprecated element usage, such as logging warnings or collecting metrics.
/// </summary>
public abstract class DeprecatedElementsValidationRule : ValidationRuleBase
{
    private readonly INodeVisitor _nodeVisitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeprecatedElementsValidationRule"/> class.
    /// </summary>
    protected DeprecatedElementsValidationRule()
    {
        _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLField>(async (node, context) =>
            {
                var fieldDef = context.TypeInfo.GetFieldDef();

                if (fieldDef?.DeprecationReason != null)
                {
                    // Get the parent type for context
                    var parentType = context.TypeInfo.GetParentType()!.GetNamedType();

                    // Call the abstract method for handling deprecated field usage
                    await OnDeprecatedFieldReferenced(context, node, fieldDef, parentType).ConfigureAwait(false);
                }
            }),
            new MatchingNodeVisitor<GraphQLArgument>(async (node, context) =>
            {
                var argumentDef = context.TypeInfo.GetArgument();

                if (argumentDef?.DeprecationReason != null)
                {
                    // Get the field definition and directive for context
                    var fieldDef = context.TypeInfo.GetFieldDef();
                    var directiveDef = context.TypeInfo.GetDirective();

                    if (directiveDef != null)
                    {
                        // This is a directive argument
                        await OnDeprecatedDirectiveArgumentReferenced(context, node, argumentDef, directiveDef).ConfigureAwait(false);
                    }
                    else if (fieldDef != null) // note: when a directive is applied to a field, both fieldDef and directiveDef are non-null
                    {
                        // This is a field argument
                        var parentType = context.TypeInfo.GetParentType()!.GetNamedType();
                        await OnDeprecatedFieldArgumentReferenced(context, node, argumentDef, fieldDef, parentType).ConfigureAwait(false);
                    }
                }
            }),
            new MatchingNodeVisitor<GraphQLFragmentDefinition>(async (node, context) =>
            {
                var typeName = node.TypeCondition.Type.Name;
                var type = context.Schema.AllTypes[typeName];

                if (type?.DeprecationReason != null)
                {
                    // Call the abstract method for handling deprecated type usage
                    await OnDeprecatedTypeReferenced(context, node.TypeCondition.Type, type).ConfigureAwait(false);
                }
            }),
            new MatchingNodeVisitor<GraphQLInlineFragment>(async (node, context) =>
            {
                if (node.TypeCondition != null)
                {
                    var typeName = node.TypeCondition.Type.Name;
                    var type = context.Schema.AllTypes[typeName];

                    if (type?.DeprecationReason != null)
                    {
                        // Call the abstract method for handling deprecated type usage
                        await OnDeprecatedTypeReferenced(context, node.TypeCondition.Type, type).ConfigureAwait(false);
                    }
                }
            })
        );
    }

    /// <inheritdoc/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    /// <summary>
    /// Called when a deprecated field is referenced in the document.
    /// Implement this method to define custom behavior for deprecated field usage.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="fieldNode">The GraphQL field node that references the deprecated field.</param>
    /// <param name="fieldDefinition">The field definition that is deprecated.</param>
    /// <param name="parentType">The parent type containing the deprecated field.</param>
    protected abstract ValueTask OnDeprecatedFieldReferenced(
        ValidationContext context,
        GraphQLField fieldNode,
        FieldType fieldDefinition,
        IGraphType parentType);

    /// <summary>
    /// Called when a deprecated field argument is referenced in the document.
    /// Implement this method to define custom behavior for deprecated field argument usage.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="argumentNode">The GraphQL argument node that references the deprecated argument.</param>
    /// <param name="argumentDefinition">The argument definition that is deprecated.</param>
    /// <param name="fieldDefinition">The field definition containing the deprecated argument.</param>
    /// <param name="parentType">The parent type containing the field with the deprecated argument.</param>
    protected abstract ValueTask OnDeprecatedFieldArgumentReferenced(
        ValidationContext context,
        GraphQLArgument argumentNode,
        QueryArgument argumentDefinition,
        FieldType fieldDefinition,
        IGraphType parentType);

    /// <summary>
    /// Called when a deprecated directive argument is referenced in the document.
    /// Implement this method to define custom behavior for deprecated directive argument usage.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="argumentNode">The GraphQL argument node that references the deprecated argument.</param>
    /// <param name="argumentDefinition">The argument definition that is deprecated.</param>
    /// <param name="directiveDefinition">The directive definition containing the deprecated argument.</param>
    protected abstract ValueTask OnDeprecatedDirectiveArgumentReferenced(
        ValidationContext context,
        GraphQLArgument argumentNode,
        QueryArgument argumentDefinition,
        Directive directiveDefinition);

    /// <summary>
    /// Called when a deprecated type is referenced in the document.
    /// Implement this method to define custom behavior for deprecated type usage.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="typeConditionNode">The GraphQL type condition node that references the deprecated type.</param>
    /// <param name="typeDefinition">The type definition that is deprecated.</param>
    protected abstract ValueTask OnDeprecatedTypeReferenced(
        ValidationContext context,
        GraphQLNamedType typeConditionNode,
        IGraphType typeDefinition);
}
