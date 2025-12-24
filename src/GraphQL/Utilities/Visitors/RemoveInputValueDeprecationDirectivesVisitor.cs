using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Removes the <c>@deprecated</c> directive from input values (arguments and input fields) within an AST.
/// </summary>
public sealed class RemoveInputValueDeprecationDirectivesVisitor : ASTVisitor<NullVisitorContext>
{
    private static readonly RemoveInputValueDeprecationDirectivesVisitor _instance = new();

    private RemoveInputValueDeprecationDirectivesVisitor()
    {
    }

    /// <inheritdoc cref="RemoveInputValueDeprecationDirectivesVisitor"/>
    public static void Visit(ASTNode node)
    {
        _instance.VisitAsync(node, default).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    protected override ValueTask VisitInputValueDefinitionAsync(GraphQLInputValueDefinition inputValue, NullVisitorContext context)
    {
        if (inputValue.Directives != null)
        {
            var deprecatedDirective = inputValue.Directives.Items.Find(d => d.Name.Value == "deprecated");
            if (deprecatedDirective != null)
            {
                inputValue.Directives.Items.Remove(deprecatedDirective);
                if (inputValue.Directives.Items.Count == 0)
                {
                    inputValue.Directives = null;
                }
            }
        }

        return base.VisitInputValueDefinitionAsync(inputValue, context);
    }
}
