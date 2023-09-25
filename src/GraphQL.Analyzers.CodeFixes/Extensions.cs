using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GraphQL.Analyzers;

public static class Extensions
{
    public static void RemoveMethodPreservingTrailingTrivia(
        this DocumentEditor docEditor,
        MemberAccessExpressionSyntax memberAccessExpression) =>
        docEditor.ReplacePreservingTrailingTrivia(
            (InvocationExpressionSyntax)memberAccessExpression.Parent!,
            (InvocationExpressionSyntax)memberAccessExpression.Expression);

    public static void ReplacePreservingTrailingTrivia(
        this DocumentEditor docEditor,
        InvocationExpressionSyntax invocationExpression,
        InvocationExpressionSyntax newInvocationExpression)
    {
        var trailingTrivia = invocationExpression.ArgumentList.CloseParenToken.TrailingTrivia;

        var updatedInvocationExpression = newInvocationExpression
            .WithArgumentList(newInvocationExpression.ArgumentList
                .WithTrailingTrivia(trailingTrivia));

        docEditor.ReplaceNode(invocationExpression, updatedInvocationExpression);
    }

    public static MemberAccessExpressionSyntax WithNewLine(this MemberAccessExpressionSyntax syntax, bool condition) =>
        !condition
            ? syntax
            : syntax.WithOperatorToken(
                Token(
                    TriviaList(
                        CarriageReturnLineFeed,
                        Whitespace("    ")),
                    SyntaxKind.DotToken,
                    TriviaList()));
}
