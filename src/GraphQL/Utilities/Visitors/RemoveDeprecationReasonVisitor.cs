using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Removes the reason from @deprecated directives within an AST.
/// </summary>
public sealed class RemoveDeprecationReasonVisitor : ASTVisitor<RemoveDeprecationReasonVisitor.Context>
{
    private static readonly RemoveDeprecationReasonVisitor _instance = new();

    private RemoveDeprecationReasonVisitor()
    {
    }

    /// <summary>
    /// Removes the reason from @deprecated directives within an AST.
    /// </summary>
    public static void Visit(ASTNode node)
    {
        _instance.VisitAsync(node, default).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    protected override ValueTask VisitDirectiveAsync(GraphQLDirective directive, Context context)
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

    /// <summary>
    /// An <see cref="IASTVisitorContext"/> implementation that does nothing.
    /// </summary>
    public struct Context : IASTVisitorContext
    {
        /// <inheritdoc/>
        public CancellationToken CancellationToken => default;
    }
}
