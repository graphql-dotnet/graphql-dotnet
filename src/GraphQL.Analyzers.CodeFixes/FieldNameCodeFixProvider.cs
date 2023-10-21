using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace GraphQL.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldNameCodeFixProvider))]
public class FieldNameCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(
            FieldNameAnalyzer.DefineTheNameInFieldMethod.Id,
            FieldNameAnalyzer.NameMethodInvocationCanBeRemoved.Id,
            FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            string builderMethodName = diagnostic.Properties[Constants.Properties.BuilderMethodName]!;

            var nameInvocationExpression = (InvocationExpressionSyntax)root!.FindNode(diagnosticSpan);
            var nameMemberAccess = (MemberAccessExpressionSyntax)nameInvocationExpression.Expression;

            if (diagnostic.Id == FieldNameAnalyzer.DefineTheNameInFieldMethod.Id)
            {
                const string codeFixTitle = "Copy name to 'Field' method";

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: codeFixTitle,
                        createChangedDocument: ct =>
                            CopyNameArgumentAsync(context.Document, nameMemberAccess, builderMethodName, ct),
                        equivalenceKey: codeFixTitle),
                    diagnostic);
            }
            else if (diagnostic.Id == FieldNameAnalyzer.NameMethodInvocationCanBeRemoved.Id)
            {
                const string codeFixTitle = "Remove redundant 'Name' method invocation";
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: codeFixTitle,
                        createChangedDocument: ct =>
                            RemoveRedundantNameInvocationAsync(context.Document, nameMemberAccess, ct),
                        equivalenceKey: codeFixTitle),
                    diagnostic);
            }
            else if (diagnostic.Id == FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods.Id)
            {
                const string codeFixTitle1 = "Use the name provided by 'Name' method";
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: codeFixTitle1,
                        createChangedDocument: ct =>
                            RemoveRedundantNameInvocationAsync(context.Document, nameMemberAccess, ct),
                        equivalenceKey: codeFixTitle1),
                    diagnostic);

                const string codeFixTitle2 = "Use the name provided by 'Field' method";
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: codeFixTitle2,
                        createChangedDocument: ct =>
                            CopyNameArgumentAsync(context.Document, nameMemberAccess, builderMethodName, ct),
                        equivalenceKey: codeFixTitle2),
                    diagnostic);
            }
        }
    }

    private static async Task<Document> CopyNameArgumentAsync(
        Document document,
        MemberAccessExpressionSyntax nameMemberAccess,
        string builderMethodName,
        CancellationToken cancellationToken)
    {
        var docEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var fieldInvocationExpression = nameMemberAccess.FindMethodInvocationExpression(builderMethodName)!;

        var fieldNameArg = fieldInvocationExpression.GetMethodArgument(Constants.ArgumentNames.Name, docEditor.SemanticModel);
        var nameArgList = ((InvocationExpressionSyntax)nameMemberAccess.Parent!).ArgumentList;

        MemberAccessExpressionSyntax newNameMemberAccess;
        // Field("yyy").Name("xxx") => Field("xxx").Name("xxx")
        if (fieldNameArg != null)
        {
            var nameArg = nameArgList.Arguments.First();
            newNameMemberAccess = nameMemberAccess.ReplaceNode(fieldNameArg.Expression, nameArg.Expression);
        }
        else // Field().Name("xxx") => Field("xxx").Name("xxx")
        {
            var fieldArgList = fieldInvocationExpression.ArgumentList;
            var newFieldArgList = nameArgList.AddArguments(fieldArgList.Arguments.ToArray());
            var newFieldInvocationExpression = fieldInvocationExpression.WithArgumentList(newFieldArgList);
            newNameMemberAccess = nameMemberAccess.ReplaceNode(fieldInvocationExpression, newFieldInvocationExpression);
        }

        // Field("xxx").Name("xxx") => Field("xxx")
        docEditor.ReplacePreservingTrailingTrivia(
            (InvocationExpressionSyntax)nameMemberAccess.Parent,
            (InvocationExpressionSyntax)newNameMemberAccess.Expression);

        return docEditor.GetChangedDocument();
    }

    private static async Task<Document> RemoveRedundantNameInvocationAsync(
        Document document,
        MemberAccessExpressionSyntax nameMemberAccess,
        CancellationToken cancellationToken)
    {
        var docEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        docEditor.RemoveMethodPreservingTrailingTrivia(nameMemberAccess);
        return docEditor.GetChangedDocument();
    }
}
