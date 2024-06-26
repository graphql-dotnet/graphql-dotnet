using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace GraphQL.Analyzers.Helpers;

public static class DocumentEditorExtensions
{
    /// <summary>
    /// Removes a method call represented by the <paramref name="memberAccessExpression"/>
    /// from the <see cref="DocumentEditor"/> while preserving trailing trivia.
    /// </summary>
    /// <param name="docEditor">The <see cref="DocumentEditor"/> instance.</param>
    /// <param name="memberAccessExpression">The <see cref="MemberAccessExpressionSyntax"/> representing the method call to be removed.</param>
    public static void RemoveMethodPreservingTrailingTrivia(
        this DocumentEditor docEditor,
        MemberAccessExpressionSyntax memberAccessExpression) =>
        docEditor.ReplacePreservingTrailingTrivia(
            (InvocationExpressionSyntax)memberAccessExpression.Parent!,
            (InvocationExpressionSyntax)memberAccessExpression.Expression);

    /// <summary>
    /// Replaces an <see cref="InvocationExpressionSyntax"/> with a new <see cref="InvocationExpressionSyntax"/>
    /// while preserving trailing trivia in the argument list.
    /// </summary>
    /// <param name="docEditor">The <see cref="DocumentEditor"/> instance.</param>
    /// <param name="invocationExpression">The original <see cref="InvocationExpressionSyntax"/> to be replaced.</param>
    /// <param name="newInvocationExpression">The new <see cref="InvocationExpressionSyntax"/> to replace the original one.</param>
    public static void ReplacePreservingTrailingTrivia(
        this DocumentEditor docEditor,
        InvocationExpressionSyntax invocationExpression,
        InvocationExpressionSyntax newInvocationExpression)
    {
        // Preserve trailing trivia from the original argument list.
        var trailingTrivia = invocationExpression.ArgumentList.CloseParenToken.TrailingTrivia;

        // Create the updated invocation expression with the preserved trailing trivia.
        var updatedInvocationExpression = newInvocationExpression
            .WithArgumentList(newInvocationExpression.ArgumentList
                .WithTrailingTrivia(trailingTrivia));

        // Replace the original invocation expression with the updated one.
        docEditor.ReplaceNode(invocationExpression, updatedInvocationExpression);
    }
}
