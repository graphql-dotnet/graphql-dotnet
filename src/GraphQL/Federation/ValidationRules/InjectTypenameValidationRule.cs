using GraphQL.Federation.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Federation;

/// <summary>
/// Injects the <c>__typename</c> field into the selection set of a GraphQL Federation entity resolver.
/// </summary>
[Obsolete("This class will be removed in GraphQL.NET v9.")]
public class InjectTypenameValidationRule : IValidationRule, INodeVisitor
{
    /// <inheritdoc/>
    public ValueTask EnterAsync(ASTNode node, ValidationContext context)
    {
        if (node is GraphQLField field && field.Name.Value == "_entities" && field.SelectionSet != null)
        {
            var lastType = context.TypeInfo.GetLastType()?.GetNamedType();
            if (lastType is EntityGraphType || lastType?.Name == "_Entity")
            {
                InjectTypename(field.SelectionSet);
            }
        }
        return default;
    }

    private static void InjectTypename(GraphQLSelectionSet selectionSet)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is GraphQLField field && field.Name.Value == "__typename")
                return;
        }
        selectionSet.Selections.Insert(0, new GraphQLField(new("__typename")));
    }

    /// <inheritdoc/>
    public ValueTask<INodeVisitor?> GetPostNodeVisitorAsync(ValidationContext context) => default;

    /// <inheritdoc/>
    public ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(this);

    /// <inheritdoc/>
    public ValueTask<IVariableVisitor?> GetVariableVisitorAsync(ValidationContext context) => default;

    /// <inheritdoc/>
    public ValueTask LeaveAsync(ASTNode node, ValidationContext context) => default;
}
