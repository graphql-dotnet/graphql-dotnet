using GraphQL.Language.AST;

namespace GraphQL.Language
{
    /// <summary>
    /// Provides helper methods for setting location information on document nodes.
    /// </summary>
    public static class NodeExtensions
    {
        /// <summary>
        /// Sets location information on a specified node and then returns the node.
        /// </summary>
        public static T WithLocation<T>(this T node, int line, int column, int start = -1, int end = -1)
            where T : AbstractNode
        {
            node.SourceLocation = new SourceLocation(line, column, start, end);
            return node;
        }
    }
}
