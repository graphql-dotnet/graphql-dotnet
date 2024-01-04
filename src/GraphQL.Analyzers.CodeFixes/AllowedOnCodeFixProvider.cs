using System.Collections.Immutable;
using System.Composition;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace GraphQL.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AllowedOnCodeFixProvider))]
public class AllowedOnCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(AllowedOnAnalyzer.IllegalMethodOrPropertyUsage.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var memberInvocationExpression = (InvocationExpressionSyntax)root!.FindNode(diagnosticSpan);
            var memberAccess = (MemberAccessExpressionSyntax)memberInvocationExpression.Expression;

            const string codeFixTitle = "Remove invalid invocation";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: codeFixTitle,
                    createChangedDocument: ct =>
                        RemoveMemberAccessAsync(context.Document, memberAccess, ct),
                    equivalenceKey: codeFixTitle),
                diagnostic);
        }
    }

    private static async Task<Document> RemoveMemberAccessAsync(
        Document document,
        MemberAccessExpressionSyntax memberAccessExpression,
        CancellationToken cancellationToken)
    {
        var docEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        docEditor.RemoveMethodPreservingTrailingTrivia(memberAccessExpression);

        return docEditor.GetChangedDocument();
    }
}
