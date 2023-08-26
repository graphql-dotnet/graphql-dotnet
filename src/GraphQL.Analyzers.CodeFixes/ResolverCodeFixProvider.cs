using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace GraphQL.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ResolverCodeFixProvider))]
public class ResolverCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(ResolverAnalyzer.IllegalResolverUsage.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var resolveInvocationExpression = (InvocationExpressionSyntax)root!.FindNode(diagnosticSpan);
            var resolveMemberAccess = (MemberAccessExpressionSyntax)resolveInvocationExpression.Expression;

            const string codeFixTitle = "Remove invalid resolver";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: codeFixTitle,
                    createChangedDocument: ct =>
                        RemoveResolveMethodInvocationAsync(context.Document, resolveMemberAccess, ct),
                    equivalenceKey: codeFixTitle),
                diagnostic);
        }
    }

    private static async Task<Document> RemoveResolveMethodInvocationAsync(
        Document document,
        MemberAccessExpressionSyntax resolveInvocationExpression,
        CancellationToken cancellationToken)
    {
        var docEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        docEditor.RemoveMethodPreservingTrailingTrivia(resolveInvocationExpression);

        return docEditor.GetChangedDocument();
    }
}
