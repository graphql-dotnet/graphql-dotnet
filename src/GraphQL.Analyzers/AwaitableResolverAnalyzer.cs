using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitableResolverAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor UseAsyncResolver = new(
        id: DiagnosticIds.USE_ASYNC_RESOLVER,
        title: "Use async resolver",
        messageFormat: "Use '{0}' to register awaitable resolver",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.USE_ASYNC_RESOLVER);

    private static readonly Dictionary<string, string> _supportedMethodsMap = new()
    {
        [Constants.MethodNames.Resolve] = Constants.MethodNames.ResolveAsync,
        [Constants.MethodNames.ResolveScoped] = Constants.MethodNames.ResolveScopedAsync
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UseAsyncResolver);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
    }

    private void AnalyzeMemberAccessExpression(SyntaxNodeAnalysisContext context)
    {
        var resolveMemberAccessExpression = (MemberAccessExpressionSyntax)context.Node;
        string methodName = resolveMemberAccessExpression.Name.Identifier.Text;

        if (!_supportedMethodsMap.ContainsKey(methodName))
        {
            return;
        }

        if (!resolveMemberAccessExpression.IsGraphQLSymbol(context))
        {
            return;
        }

        var returnType = resolveMemberAccessExpression.GetFieldBuilderReturnTypeSymbol(context.SemanticModel);
        if (returnType == null)
        {
            return;
        }

        // Field<T, Task<K>> or .Return<Task<K>>
        if (returnType.IsAwaitableNonDynamic(context.SemanticModel, context.Node.SpanStart))
        {
            ReportDiagnostic(context, resolveMemberAccessExpression, methodName);
            return;
        }

        // not object and not dynamic
        if (returnType is ITypeSymbol { SpecialType: not SpecialType.System_Object, Kind: not SymbolKind.DynamicType })
        {
            return;
        }

        var invocation = resolveMemberAccessExpression.FindMethodInvocationExpression(methodName);
        var resolver = invocation?.ArgumentList.Arguments.FirstOrDefault();
        if (resolver == null)
        {
            return;
        }

        switch (resolver.Expression)
        {
            // Resolve(ctx => AsyncMethod() or statement)
            case SimpleLambdaExpressionSyntax { ExpressionBody: not null } lambda:
            {
                var expressionStatementSymbolInfo = context.SemanticModel.GetSymbolInfo(lambda.ExpressionBody);
                if (expressionStatementSymbolInfo.Symbol.IsAwaitableNonDynamic(context.SemanticModel, context.Node.SpanStart))
                {
                    ReportDiagnostic(context, resolveMemberAccessExpression, methodName);
                }

                break;
            }
            // Resolve(ctx =>
            // {
            //     return AsyncMethod() or statement
            // })
            case SimpleLambdaExpressionSyntax { Block: not null } lambda:
            {
                bool hasAwaitableStatements = lambda.Block
                    .DescendantNodesAndSelf()
                    .OfType<ReturnStatementSyntax>()
                    .Where(returnStatement => returnStatement.Expression != null)
                    .Any(returnStatement =>
                    {
                        var symbolInfo = context.SemanticModel.GetSymbolInfo(returnStatement.Expression!);
                        return symbolInfo.Symbol.IsAwaitableNonDynamic(context.SemanticModel, context.Node.SpanStart);
                    });

                if (hasAwaitableStatements)
                {
                    ReportDiagnostic(context, resolveMemberAccessExpression, methodName);
                }

                break;
            }
            // Resolve(MethodGroup)
            case IdentifierNameSyntax methodGroupName:
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(methodGroupName);
                if (symbolInfo.Symbol.IsAwaitableNonDynamic(context.SemanticModel, context.Node.SpanStart))
                {
                    ReportDiagnostic(context, resolveMemberAccessExpression, methodName);
                }

                break;
            }
        }
    }

    private static void ReportDiagnostic(
        SyntaxNodeAnalysisContext context,
        MemberAccessExpressionSyntax resolveMemberAccessExpression,
        string syncResolverName)
    {
        var location = resolveMemberAccessExpression.FindSimpleNameSyntax(syncResolverName)!.GetLocation();
        var diagnostic = Diagnostic.Create(UseAsyncResolver, location, _supportedMethodsMap[syncResolverName]);

        context.ReportDiagnostic(diagnostic);
    }
}
