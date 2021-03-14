using System;
using GraphQL.Language.AST;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for AST nodes.
    /// </summary>
    public static class AstExtensions
    {
        /// <summary>
        /// Returns the original string that was parsed into the specified node.
        /// </summary>
        public static string ToString(this INode node, Document document)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (document.OriginalQuery == null)
                throw new ArgumentNullException(nameof(document) + "." + nameof(Document.OriginalQuery));

            return document.OriginalQuery.Substring(node.SourceLocation.Start, node.SourceLocation.End - node.SourceLocation.Start);
        }
    }
}
