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
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AwaitableResolverCodeFixProvider))]
public class AwaitableResolverCodeFixProvider : CodeFixProvider
{
    public const string ASYNC_SUFFIX = "Async";
    public const string CONTEXT_PARAMETER_NAME = "context";
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(AwaitableResolverAnalyzer.UseAsyncResolver.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var resolverNameSyntax = (SimpleNameSyntax)root!.FindNode(diagnosticSpan);

            if (!await IsCodeFixSupportedAsync(context, resolverNameSyntax).ConfigureAwait(false))
            {
                continue;
            }

            const string codeFixTitle = "Replace with async method";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: codeFixTitle,
                    createChangedDocument: ct =>
                        RewriteResolverMethodAsync(context.Document, resolverNameSyntax, ct),
                    equivalenceKey: codeFixTitle),
                diagnostic);
        }
    }

    private static async Task<Document> RewriteResolverMethodAsync(
        Document document,
        SimpleNameSyntax resolverNameSyntax,
        CancellationToken cancellationToken)
    {
        var docEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var resolveInvocation = resolverNameSyntax.FindMethodInvocationExpression();
        var resolver = resolveInvocation?.ArgumentList.Arguments.FirstOrDefault();
        if (resolver == null)
        {
            return document;
        }

        switch (resolver.Expression)
        {
            // Resolve(ctx => AsyncMethod() or statement)
            // Resolve(ctx => flag ? AsyncMethod() : statement)
            case SimpleLambdaExpressionSyntax { ExpressionBody: not null } lambda:
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // Resolve(async ctx => await AsyncMethod() or statement)
                // Resolve(async ctx => flag ? await AsyncMethod() : statement)
                var newLambda = lambda
                    .WithAsyncKeyword(
                        Token(SyntaxKind.AsyncKeyword).WithLeadingTrivia(lambda.GetLeadingTrivia()))
                    .WithExpressionBody(
                        AwaitAwaitableExpressions(lambda.ExpressionBody, semanticModel!, root!.SpanStart));

                docEditor.ReplaceNode(lambda, newLambda);

                break;
            }
            // Resolve(ctx =>
            // {
            //     return AsyncMethod() or statement
            // })
            case SimpleLambdaExpressionSyntax { Block: not null } lambda:
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var rewriter = new AwaitableStatementRewriter(semanticModel!, root!);
                var newBlock = (BlockSyntax)rewriter.Visit(lambda.Block);

                var newLambda = lambda
                    .WithAsyncKeyword(
                        Token(SyntaxKind.AsyncKeyword).WithLeadingTrivia(Space))
                    .WithBlock(newBlock);

                docEditor.ReplaceNode(lambda, newLambda);
                break;
            }
            // ResolveAsync(MethodGroup)
            case IdentifierNameSyntax methodGroupName:
            {
                // Resolve(async context => await MethodGroup(context))
                var newLambda = SyntaxFactory
                    .SimpleLambdaExpression(
                        Parameter(Identifier(CONTEXT_PARAMETER_NAME)))
                    .WithAsyncKeyword(
                        Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(Space))
                    .WithExpressionBody(
                        AwaitExpression(
                                Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(Space),
                                InvocationExpression(
                                        IdentifierName(methodGroupName.Identifier))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    IdentifierName(CONTEXT_PARAMETER_NAME)))))));

                docEditor.ReplaceNode(methodGroupName, newLambda);
                break;
            }
            default:
                return document;
        }

        // Resolve(...) => ResolveAsync(...)
        var newResolverNameSyntax = IdentifierName(resolverNameSyntax.Identifier.Text + ASYNC_SUFFIX);
        docEditor.ReplaceNode(resolverNameSyntax, newResolverNameSyntax);

        return docEditor.GetChangedDocument();
    }

    private static async Task<bool> IsCodeFixSupportedAsync(
        CodeFixContext context,
        SimpleNameSyntax resolverNameSyntax)
    {
        if (resolverNameSyntax.Identifier.Text == Constants.MethodNames.ResolveScoped)
        {
            // currently not supported
            return false;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var returnType = resolverNameSyntax.GetFieldBuilderReturnTypeSymbol(semanticModel!);
        if (returnType.IsAwaitableNonDynamic(semanticModel!, root!.SpanStart))
        {
            // Field<T, Task<K>> or .Return<Task<K>>
            return false;
        }

        return true;
    }

    /// <summary>
    /// Wraps the awaitable parts of the <paramref name="expression"/> into the <c>await</c> expression.
    /// Parentheses are unwrapped and both branches of a conditional expression are rewritten independently,
    /// so <c>flag ? AsyncMethod() : "text"</c> becomes <c>flag ? await AsyncMethod() : "text"</c>.
    /// Conditional expressions may be nested within each other.
    /// </summary>
    private static ExpressionSyntax AwaitAwaitableExpressions(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        int position)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                return parenthesized.WithExpression(
                    AwaitAwaitableExpressions(parenthesized.Expression, semanticModel, position));

            case ConditionalExpressionSyntax conditional:
                return conditional
                    .WithWhenTrue(AwaitAwaitableExpressions(conditional.WhenTrue, semanticModel, position))
                    .WithWhenFalse(AwaitAwaitableExpressions(conditional.WhenFalse, semanticModel, position));

            default:
                return expression.IsAwaitableExpression(semanticModel, position)
                    ? AwaitExpression(
                            Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(Space),
                            expression.WithoutLeadingTrivia())
                        .WithLeadingTrivia(expression.GetLeadingTrivia())
                    : expression;
        }
    }

    private class AwaitableStatementRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;
        private readonly SyntaxNode _rootNode;

        public AwaitableStatementRewriter(SemanticModel semanticModel, SyntaxNode rootNode)
        {
            _semanticModel = semanticModel;
            _rootNode = rootNode;
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax returnStatement)
        {
            if (returnStatement.Expression == null)
            {
                return returnStatement;
            }

            return !returnStatement.Expression.IsAwaitableExpression(_semanticModel, _rootNode.SpanStart)
                ? returnStatement
                : returnStatement.WithExpression(
                    AwaitAwaitableExpressions(returnStatement.Expression, _semanticModel, _rootNode.SpanStart));
        }
    }
}
