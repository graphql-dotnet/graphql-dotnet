using System.Collections.Immutable;
using System.Composition;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace GraphQL.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableReferenceTypeArgumentCodeFixProvider))]
public class NullableReferenceTypeArgumentCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.NULLABLE_REFERENCE_TYPE_ARGUMENT_SHOULD_SPECIFY_NULLABLE);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var typeArgumentSyntax = root!.FindNode(diagnosticSpan);

            // Find the invocation expression
            var invocationExpr = typeArgumentSyntax.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocationExpr == null)
                continue;

            const string codeFixTitle = "Add nullable: true";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: codeFixTitle,
                    createChangedDocument: ct =>
                        AddNullableArgumentAsync(context.Document, invocationExpr, ct),
                    equivalenceKey: codeFixTitle),
                diagnostic);
        }
    }

    private static async Task<Document> AddNullableArgumentAsync(
        Document document,
        InvocationExpressionSyntax invocationExpr,
        CancellationToken cancellationToken)
    {
        var docEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Get the method symbol to find the nullable parameter
        if (semanticModel!.GetSymbolInfo(invocationExpr, cancellationToken).Symbol is not IMethodSymbol methodSymbol)
            return document;

        var nullableParam = methodSymbol.Parameters.FirstOrDefault(p => p.Name == Constants.ArgumentNames.Nullable);
        if (nullableParam == null)
            return document;

        var argumentList = invocationExpr.ArgumentList;
        var existingNullableArg = invocationExpr.GetMethodArgument(Constants.ArgumentNames.Nullable, semanticModel!);

        var trueExpression = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

        if (existingNullableArg != null)
        {
            // Replace existing null/default with true, preserving whether it's named or positional
            var newArg = existingNullableArg.WithExpression(trueExpression);
            docEditor.ReplaceNode(existingNullableArg, newArg);
        }
        else
        {
            // Add new nullable: true argument as a named parameter
            var newArg = SyntaxFactory.Argument(
                SyntaxFactory.NameColon(Constants.ArgumentNames.Nullable),
                default,
                trueExpression);

            // Add at the end of the argument list
            var newArgumentList = argumentList.AddArguments(newArg);
            docEditor.ReplaceNode(argumentList, newArgumentList);
        }

        return docEditor.GetChangedDocument();
    }
}
