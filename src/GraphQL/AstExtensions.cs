using System;
using GraphQLParser.AST;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for AST nodes.
    /// </summary>
    public static class AstExtensions
    {
        /// <summary>
        /// Returns the original string that was parsed from the provided document into the specified node.
        /// </summary>
        public static string StringFrom(this ASTNode node, string? originalQuery)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (originalQuery == null)
                throw new ArgumentNullException(nameof(originalQuery));

            return originalQuery.Substring(node.Location.Start, node.Location.End - node.Location.Start);
        }
    }
}
