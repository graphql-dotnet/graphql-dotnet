using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Removes all descriptions from an AST.
/// </summary>
public class RemoveDescriptionVisitor : ASTVisitor<RemoveDescriptionVisitor.Context>
{
    private static readonly RemoveDescriptionVisitor _instance = new();

    private RemoveDescriptionVisitor()
    {
    }

    /// <summary>
    /// Removes all descriptions from an AST.
    /// </summary>
    public static void Visit(ASTNode node)
    {
        _ = _instance.VisitAsync(node, default);
    }

    /// <inheritdoc/>
    public override ValueTask VisitAsync(ASTNode? node, Context context)
    {
        if (node is IHasDescriptionNode descriptionNode)
        {
            descriptionNode.Description = null;
        }
        return base.VisitAsync(node, context);
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
