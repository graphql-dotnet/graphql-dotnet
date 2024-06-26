using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GraphQL.Analyzers.Helpers;

public static class EnumerableExtensions
{
    /// <summary>
    /// Converts the provided <see cref="SyntaxNode"/> collection
    /// to <see cref="SeparatedSyntaxList{TNode}"/> with specified separator.
    /// </summary>
    /// <typeparam name="T">The element type</typeparam>
    /// <param name="nodes">Source collection to convert into <see cref="SeparatedSyntaxList{TNode}"/>.</param>
    /// <param name="separator">The separator token. Default is <see cref="SyntaxKind.CommaToken"/></param>
    public static SeparatedSyntaxList<T> ToSeparatedList<T>(
        this IEnumerable<T>? nodes,
        SyntaxKind separator = SyntaxKind.CommaToken)
        where T : SyntaxNode
    {
        var nodesList = nodes == null ? [] : nodes.ToList();
        return SyntaxFactory.SeparatedList(
            nodesList,
            Enumerable.Repeat(SyntaxFactory.Token(separator), Math.Max(nodesList.Count - 1, 0)));
    }
}
