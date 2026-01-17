using System.Collections.Immutable;
using System.Composition;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace GraphQL.Analyzers.Federation;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DuplicateKeyCodeFixProvider))]
public class DuplicateKeyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.DUPLICATE_KEY);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            if (root!.FindNode(diagnosticSpan) is not InvocationExpressionSyntax invocationExpression)
                continue;

            const string codeFixTitle = "Remove duplicate key";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: codeFixTitle,
                    createChangedDocument: ct =>
                        RemoveDuplicateKeyAsync(context.Document, invocationExpression, ct),
                    equivalenceKey: codeFixTitle),
                diagnostic);
        }
    }

    private static async Task<Document> RemoveDuplicateKeyAsync(
        Document document,
        InvocationExpressionSyntax invocationExpression,
        CancellationToken cancellationToken)
    {
        var docEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Find the statement containing the invocation
        var statement = invocationExpression.FirstAncestorOrSelf<StatementSyntax>();
        if (statement != null)
        {
            docEditor.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
        }

        return docEditor.GetChangedDocument();
    }
}
