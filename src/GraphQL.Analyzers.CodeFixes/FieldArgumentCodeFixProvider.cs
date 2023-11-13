using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GraphQL.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldArgumentCodeFixProvider))]
public class FieldArgumentCodeFixProvider : CodeFixProvider
{
    private const string LAMBDA_EXPRESSION_PARAMETER_NAME = "argument";

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(FieldArgumentAnalyzer.DoNotUseObsoleteArgumentMethod.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var argumentInvocation = (InvocationExpressionSyntax)root!.FindNode(diagnosticSpan);

            const string codeFixTitle = "Rewrite obsolete 'Argument' method";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: codeFixTitle,
                    createChangedDocument: ct =>
                        RewriteArgumentMethodAsync(context.Document, argumentInvocation, ct),
                    equivalenceKey: codeFixTitle),
                diagnostic);
        }
    }

    private static async Task<Document> RewriteArgumentMethodAsync(
        Document document,
        InvocationExpressionSyntax argumentInvocation,
        CancellationToken cancellationToken)
    {
        var docEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var argumentMemberAccess = (MemberAccessExpressionSyntax)argumentInvocation.Expression;
        if (argumentMemberAccess.Name is not GenericNameSyntax genericName)
        {
            return document;
        }

        if (genericName.TypeArgumentList.Arguments.Count != 2)
        {
            return document;
        }

        var newMethodName = genericName
            .WithTypeArgumentList(
                RewriteTypeArgumentList(genericName));

        docEditor.ReplaceNode(argumentMemberAccess.Name, newMethodName);

        var semanticModel = (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
        var defaultValueArg = argumentInvocation.GetMethodArgument(Constants.ArgumentNames.DefaultValue, semanticModel);

        // .Argument<T>(x, y) or .Argument<T>(x, y, configure: arg => ...)
        if (defaultValueArg == null)
        {
            return docEditor.GetChangedDocument();
        }

        var configureArg = argumentInvocation.GetMethodArgument(Constants.ArgumentNames.Configure, semanticModel);

        // .Argument<T>(x, y, defaultValue)
        if (configureArg == null)
        {
            var configureLambdaArg = CreateConfigureLambdaArgument(defaultValueArg);
            docEditor.ReplaceNode(defaultValueArg, configureLambdaArg);
            return docEditor.GetChangedDocument();
        }

        if (configureArg.Expression is not SimpleLambdaExpressionSyntax configureLambda)
        {
            return document;
        }

        // .Argument<,>(x, y, defaultValue, arg => ...)
        // .Argument<,>(x, y, arg => ...)
        // .Argument<>(x, y, arg => ...)
        SimpleLambdaExpressionSyntax? newConfigureLambda;

        // arg => { arg.Prop = val; }
        if (configureLambda.Block != null)
        {
            newConfigureLambda = AddDefaultValueToConfigureLambdaBlock(configureLambda);
        }
        // arg => arg.Prop = val
        else if (configureLambda.ExpressionBody != null)
        {
            newConfigureLambda = ConvertBodyConfigureLambdaToBlockLambda(configureLambda);
        }
        else
        {
            // theoretically should never happen
            return document;
        }

        var newMethodArgumentList = argumentInvocation.ArgumentList
            .Arguments
            .Insert(2, Argument(newConfigureLambda))
            .RemoveAt(4)
            .RemoveAt(3);

        var newArgumentsList = argumentInvocation.ArgumentList.WithArguments(newMethodArgumentList);
        docEditor.ReplaceNode(argumentInvocation.ArgumentList, newArgumentsList);

        return docEditor.GetChangedDocument();

        SimpleLambdaExpressionSyntax AddDefaultValueToConfigureLambdaBlock(SimpleLambdaExpressionSyntax lambda)
        {
            var (leadingTrivia, trailingTrivia) = GetBlockStatementTrivia(lambda.Block!);
            var defaultValueStatement = CreateDefaultValueStatement(leadingTrivia, defaultValueArg, trailingTrivia);

            return lambda.WithBlock(
                lambda.Block!.AddStatements(defaultValueStatement));
        }

        SimpleLambdaExpressionSyntax ConvertBodyConfigureLambdaToBlockLambda(SimpleLambdaExpressionSyntax lambda)
        {
            var block = SyntaxFactory
                .Block(
                    ExpressionStatement(lambda.ExpressionBody!.WithoutTrailingTrivia())
                        .WithLeadingTrivia(Space),
                    ExpressionStatement(
                            CreateDefaultValuePropertyAssignment(lambda.Parameter.ToString(), defaultValueArg.Expression))
                        .WithLeadingTrivia(Space)
                        .WithTrailingTrivia(Space))
                .WithLeadingTrivia(lambda.ExpressionBody!.GetLeadingTrivia());

            return SimpleLambdaExpression(lambda.Parameter, block)
                .WithArrowToken(lambda.ArrowToken);
        }
    }

    private static (SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia) GetBlockStatementTrivia(BlockSyntax block)
    {
        var lastStatement = block.Statements.LastOrDefault();

        // argument =>
        // {
        //     statement;
        // }
        // or
        // argument => { statement; }
        if (lastStatement != null)
        {
            return (lastStatement.GetLeadingTrivia(), lastStatement.GetTrailingTrivia());
        }

        // argument => { }
        if (block.OpenBraceToken.GetLocation().GetLineSpan().StartLinePosition.Line ==
            block.CloseBraceToken.GetLocation().GetLineSpan().StartLinePosition.Line)
        {
            return (SyntaxTriviaList.Empty, SyntaxTriviaList.Empty);
        }

        // argument =>
        // {
        // }
        var leadingTrivia = block.GetLeadingTrivia().Add(Whitespace("    "));

        return (leadingTrivia, new SyntaxTriviaList(EndOfLine(Environment.NewLine)));
    }

    // {whitespaces}argument.DefaultValue = xxx;\n
    private static ExpressionStatementSyntax CreateDefaultValueStatement(
        SyntaxTriviaList leadingTrivia,
        ArgumentSyntax defaultValueArg,
        SyntaxTriviaList trailingTrivia) =>
        ExpressionStatement(CreateDefaultValuePropertyAssignment(defaultValueArg.Expression))
            .WithLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia(trailingTrivia);

    // argument => argument.DefaultValue = expression
    private static ArgumentSyntax CreateConfigureLambdaArgument(ArgumentSyntax defaultValueArg)
    {
        var configureLambda = ParseExpression(
                $"{LAMBDA_EXPRESSION_PARAMETER_NAME} => {CreateDefaultValuePropertyAssignment(defaultValueArg.Expression)}")
            .WithLeadingTrivia(defaultValueArg.GetLeadingTrivia());

        return Argument(configureLambda);
    }

    // argument.DefaultValue = expression
    private static ExpressionSyntax CreateDefaultValuePropertyAssignment(ExpressionSyntax expression) =>
        CreateDefaultValuePropertyAssignment(LAMBDA_EXPRESSION_PARAMETER_NAME, expression);

    // parameterName.DefaultValue = expression
    private static ExpressionSyntax CreateDefaultValuePropertyAssignment(string parameterName, ExpressionSyntax expression) =>
        ParseExpression($"{parameterName}.{Constants.ObjectProperties.DefaultValue} = {expression}");

    // from Argument<TArgumentGraphType, TArgumentType>
    // to   Argument<TArgumentGraphType>
    private static TypeArgumentListSyntax RewriteTypeArgumentList(GenericNameSyntax genericName) =>
        genericName.TypeArgumentList
            .WithArguments(
                SingletonSeparatedList(
                    genericName.TypeArgumentList.Arguments.First()));
}
