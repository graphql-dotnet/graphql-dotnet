using System.Collections.Immutable;
using System.Composition;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GraphQL.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldBuilderCodeFixProvider))]
public class FieldBuilderCodeFixProvider : CodeFixProvider
{
    public static string ReformatOption { get; } = $"dotnet_diagnostic.{FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods.Id}.reformat";

    public static string SkipNullsOption { get; } = $"dotnet_diagnostic.{FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods.Id}.skip_nulls";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            bool isAsyncField = diagnostic.Properties.ContainsKey(Constants.AnalyzerProperties.IsAsync);
            bool isDelegate = diagnostic.Properties.ContainsKey(Constants.AnalyzerProperties.IsDelegate);

            var fieldInvocationExpression = (InvocationExpressionSyntax)root!.FindNode(diagnosticSpan);

            const string codeFixTitle = "Rewrite obsolete 'Field' method";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: codeFixTitle,
                    createChangedDocument: ct =>
                        RewriteFieldBuilderAsync(context.Document, fieldInvocationExpression, isAsyncField, isDelegate, ct),
                    equivalenceKey: codeFixTitle),
                diagnostic);
        }
    }

    private static async Task<Document> RewriteFieldBuilderAsync(
        Document document,
        InvocationExpressionSyntax fieldInvocationExpression,
        bool isAsyncField,
        bool isDelegate,
        CancellationToken cancellationToken)
    {
        var docEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;

        var tree = (await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false))!;
        bool reformat = document.Project.AnalyzerOptions.GetBoolOption(
            ReformatOption,
            tree);

        bool skipNulls = document.Project.AnalyzerOptions.GetBoolOption(
            SkipNullsOption,
            tree,
            defaultValue: true);

        // first handle field name and type
        var nameArg = fieldInvocationExpression.GetMethodArgument(Constants.ArgumentNames.Name, semanticModel);
        var typeArg = fieldInvocationExpression.GetMethodArgument(Constants.ArgumentNames.Type, semanticModel);

        var newFieldInvocationExpression = CreateFieldInvocationExpression(
            fieldInvocationExpression,
            nameArg!.Expression,
            typeArg?.Expression);

        const int oneLevelIndentation = 4;
        int fieldInvocationIndentation = newFieldInvocationExpression
            .GetLeadingTrivia()
            .LastOrDefault(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            .FullSpan.Length;

        var whitespaceTrivia = Whitespace(new string(' ', fieldInvocationIndentation + oneLevelIndentation));
        var newLineTrivia = EndOfLine(Environment.NewLine);

        // now handle rest of the arguments
        var args = GetArgumentsWithNames(fieldInvocationExpression, semanticModel);

        foreach ((string name, var arg, bool newLine) in args)
        {
            string? invocationName = null;
            switch (name)
            {
                case Constants.ArgumentNames.Name:
                case Constants.ArgumentNames.Type:
                    // already handled
                    break;
                case Constants.ArgumentNames.Description:
                    invocationName = Constants.MethodNames.Description;
                    break;
                case Constants.ArgumentNames.Arguments:
                    invocationName = Constants.MethodNames.Arguments;
                    break;
                case Constants.ArgumentNames.Resolve:
                    invocationName = isDelegate
                        ? Constants.MethodNames.ResolveDelegate
                        : isAsyncField
                            ? Constants.MethodNames.ResolveAsync
                            : Constants.MethodNames.Resolve;
                    break;
                case Constants.ArgumentNames.Subscribe:
                    invocationName = isAsyncField
                        ? Constants.MethodNames.ResolveStreamAsync
                        : Constants.MethodNames.ResolveStream;
                    break;
                case Constants.ArgumentNames.DeprecationReason:
                    invocationName = Constants.MethodNames.DeprecationReason;
                    break;
            }

            if (invocationName != null)
            {
                var argLeadingTrivia = arg.GetLeadingTrivia();
                var leadingTrivia = reformat
                    ? argLeadingTrivia.Any()
                        ? TriviaList(newLineTrivia).AddRange(argLeadingTrivia)
                        : TriviaList(newLineTrivia, whitespaceTrivia)
                    : newLine
                        ? TriviaList(newLineTrivia).AddRange(argLeadingTrivia)
                        : TriviaList(whitespaceTrivia);

                newFieldInvocationExpression = CreateInvocationExpression(
                    newFieldInvocationExpression,
                    invocationName,
                    arg.Expression
                        .WithTrailingTrivia(
                            arg.Expression.GetTrailingTrivia()
                                .Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia))),
                    leadingTrivia,
                    skipNulls);
            }
        }

        docEditor.ReplaceNode(fieldInvocationExpression, newFieldInvocationExpression);

        var x = await docEditor.GetChangedDocument().GetTextAsync(cancellationToken).ConfigureAwait(false);
        var xx = x.ToString();

        return docEditor.GetChangedDocument();
    }

    private static InvocationExpressionSyntax CreateFieldInvocationExpression(
        InvocationExpressionSyntax originalFieldInvocationExpression,
        ExpressionSyntax nameArgumentExpression,
        ExpressionSyntax? typeArgumentExpression)
    {
        // replace FieldAsync, FieldSubscribe(Async) and FieldDelegate with Field
        var newExpression = OverrideName(originalFieldInvocationExpression.Expression, Constants.MethodNames.Field);

        // Field<>("name") or Field("name", type)
        var newArgumentList = typeArgumentExpression == null
            ? SingletonSeparatedList(
                Argument(nameArgumentExpression))
            : SeparatedList(new[]
            {
                Argument(nameArgumentExpression),
                Argument(typeArgumentExpression)
            });

        return originalFieldInvocationExpression
            .WithExpression(newExpression)
            .WithArgumentList(
                ArgumentList(newArgumentList));

        static ExpressionSyntax OverrideName(ExpressionSyntax exp, string newName)
        {
            return exp switch
            {
                // Field()
                SimpleNameSyntax simpleName => simpleName
                    .WithIdentifier(
                        GetIdentifier(simpleName, newName)),
                // this.Field()
                MemberAccessExpressionSyntax memberAccess => memberAccess
                    .WithName(memberAccess.Name
                        .WithIdentifier(
                            GetIdentifier(memberAccess.Name, newName))),
                _ => exp
            };
        }

        static SyntaxToken GetIdentifier(SimpleNameSyntax simpleNameSyntax, string name)
        {
            return Identifier(
                simpleNameSyntax.Identifier.LeadingTrivia,
                name,
                simpleNameSyntax.Identifier.TrailingTrivia);
        }
    }

    private static IEnumerable<(string name, ArgumentSyntax argument, bool newLine)> GetArgumentsWithNames(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        var methodSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(invocation).Symbol!;
        var @params = methodSymbol.Parameters;

        int index = 0;
        bool newLine = false;

        foreach (var nodeOrToken in invocation.ArgumentList.ChildNodesAndTokens())
        {
            if (nodeOrToken.AsNode() is ArgumentSyntax arg)
            {
                if (arg.NameColon != null)
                {
                    yield return (arg.NameColon.Name.Identifier.Text, arg, newLine);
                }
                else
                {
                    yield return (@params[index].Name, arg, newLine);
                }

                index++;
            }
            else if (nodeOrToken.IsToken)
            {
                // if any token has trailing EndOfLineTrivia, the next argument will start on the new line
                newLine = nodeOrToken
                    .AsToken()
                    .GetAllTrivia()
                    .Any(trivia => trivia.Token.TrailingTrivia
                        .Any(trailingTrivia => trailingTrivia.IsKind(SyntaxKind.EndOfLineTrivia)));
            }
        }
    }

    private static InvocationExpressionSyntax CreateInvocationExpression(
        InvocationExpressionSyntax previousExpression,
        string invocationName,
        ExpressionSyntax argumentExpression,
        SyntaxTriviaList leadingTrivia,
        bool skipNulls)
    {
        if (skipNulls && argumentExpression.IsKind(SyntaxKind.NullLiteralExpression))
        {
            return previousExpression;
        }

        // previousExpression
        //     .InvocationName(argumentExpression)
        var exp = SyntaxFactory
            .InvocationExpression(
                MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        previousExpression,
                        IdentifierName(invocationName))
                    .WithOperatorToken(
                        Token(
                            leadingTrivia,
                            SyntaxKind.DotToken,
                            TriviaList())))
            .WithArgumentList(
                ArgumentList(
                        SingletonSeparatedList(
                            Argument(argumentExpression))));

        return exp;
    }
}
