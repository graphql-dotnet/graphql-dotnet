using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Removes all descriptions from an AST.
/// </summary>
public sealed class RemoveDescriptionsVisitor : ASTVisitor<RemoveDescriptionsVisitor.Context>
{
    private static readonly RemoveDescriptionsVisitor _instance = new();

    private RemoveDescriptionsVisitor()
    {
    }

    /// <inheritdoc cref="RemoveDescriptionsVisitor"/>
    public static void Visit(ASTNode node)
    {
        _instance.VisitAsync(node, default).GetAwaiter().GetResult();
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
