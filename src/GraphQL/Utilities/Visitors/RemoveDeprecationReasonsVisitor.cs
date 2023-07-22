using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Removes the reason from <c>@deprecated</c> directives within an AST.
/// </summary>
public sealed class RemoveDeprecationReasonsVisitor : ASTVisitor<NullVisitorContext>
{
    private static readonly RemoveDeprecationReasonsVisitor _instance = new();

    private RemoveDeprecationReasonsVisitor()
    {
    }

    /// <inheritdoc cref="RemoveDeprecationReasonsVisitor"/>
    public static void Visit(ASTNode node)
    {
        _instance.VisitAsync(node, default).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    protected override ValueTask VisitDirectiveAsync(GraphQLDirective directive, NullVisitorContext context)
    {
        if (directive.Name.Value == "deprecated")
        {
            if (directive.Arguments != null)
            {
                directive.Arguments.Items.RemoveAll(a => a.Name.Value == "reason");
                if (directive.Arguments.Items.Count == 0)
                {
                    directive.Arguments = null;
                }
            }
        }
        return base.VisitDirectiveAsync(directive, context);
    }
}
