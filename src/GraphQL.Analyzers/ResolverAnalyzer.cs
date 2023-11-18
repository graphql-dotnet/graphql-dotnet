using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ResolverAnalyzer : DiagnosticAnalyzer
{
    // Violation: any of _forbiddenMethodNames used on type that doesn't implement _allowedInterfaces
    // Fixed:     remove the illegal method
    public static readonly DiagnosticDescriptor IllegalResolverUsage = new(
        id: DiagnosticIds.ILLEGAL_RESOLVER_USAGE,
        title: "Illegal resolver usage",
        messageFormat: "Resolvers are not allowed on non-output graph types",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.ILLEGAL_RESOLVER_USAGE);

    private static readonly HashSet<string> _allowedInterfaces = new()
    {
        Constants.Interfaces.IObjectGraphType
    };

    private static readonly HashSet<string> _forbiddenMethodNames = new()
    {
        Constants.MethodNames.Resolve,
        Constants.MethodNames.ResolveAsync,
        Constants.MethodNames.ResolveDelegate,
        Constants.MethodNames.ResolveScoped,
        Constants.MethodNames.ResolveScopedAsync,
        Constants.MethodNames.ResolveStream,
        Constants.MethodNames.ResolveStreamAsync
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(IllegalResolverUsage);

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

        if (!_forbiddenMethodNames.Contains(methodName))
        {
            return;
        }

        if (!resolveMemberAccessExpression.IsGraphQLSymbol(context.SemanticModel))
        {
            return;
        }

        var fieldInvocationExpression = resolveMemberAccessExpression.FindMethodInvocationExpression(Constants.MethodNames.Field);

        // without Field() invocation we know nothing about the source type
        // where FieldBuilder was created
        if (fieldInvocationExpression == null)
        {
            return;
        }

        ITypeSymbol? typeSymbol = null;

        switch (fieldInvocationExpression.Expression)
        {
            // this.Field() or graphType.Field() or Method().Field()
            case MemberAccessExpressionSyntax identifierNameSyntax:
                typeSymbol = context.SemanticModel.GetTypeInfo(identifierNameSyntax.Expression).Type;
                break;
            // Field()
            case SimpleNameSyntax:
            {
                var enclosingClass = FindEnclosingClassDeclarationSyntax(fieldInvocationExpression);
                if (enclosingClass != null)
                {
                    typeSymbol = context.SemanticModel.GetDeclaredSymbol(enclosingClass);
                }

                break;
            }
        }

        if (typeSymbol == null)
        {
            return;
        }

        bool implementsAllowedInterfaces = typeSymbol.AllInterfaces
            .Any(i => _allowedInterfaces.Contains(i.Name) && i.IsGraphQLSymbol());

        if (!implementsAllowedInterfaces)
        {
            ReportFieldTypeDiagnostic(context, resolveMemberAccessExpression, IllegalResolverUsage);
        }
    }

    private static ClassDeclarationSyntax? FindEnclosingClassDeclarationSyntax(SyntaxNode syntaxNode)
    {
        var potentialEnclosingClass = syntaxNode.Parent;

        while (potentialEnclosingClass != null && potentialEnclosingClass is not ClassDeclarationSyntax)
        {
            potentialEnclosingClass = potentialEnclosingClass.Parent;
        }

        return potentialEnclosingClass as ClassDeclarationSyntax;
    }

    private static void ReportFieldTypeDiagnostic(
        SyntaxNodeAnalysisContext context,
        MemberAccessExpressionSyntax memberAccessExpressionSyntax,
        DiagnosticDescriptor diagnosticDescriptor)
    {
        var location = memberAccessExpressionSyntax.GetMethodInvocationLocation();
        var diagnostic = Diagnostic.Create(diagnosticDescriptor, location);

        context.ReportDiagnostic(diagnostic);
    }
}
