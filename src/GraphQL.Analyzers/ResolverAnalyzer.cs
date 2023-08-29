using System.Collections.Immutable;
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
        id: "GQL005",
        title: "Illegal resolver usage",
        messageFormat: "Resolve methods are not allowed on non-output graph types",
        category: "Resolver",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.GQL005);

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

        if (!resolveMemberAccessExpression.IsGraphQLSymbol(context))
        {
            return;
        }

        var fieldInvocationExpression = resolveMemberAccessExpression.FindFieldInvocationExpression();

        // without Field() invocation we know nothing about the source type
        // where FieldBuilder was created
        if (fieldInvocationExpression == null)
        {
            return;
        }

        ITypeSymbol typeSymbol = null;

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
                typeSymbol = context.SemanticModel.GetDeclaredSymbol(enclosingClass);
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

    private ClassDeclarationSyntax FindEnclosingClassDeclarationSyntax(SyntaxNode syntaxNode)
    {
        var potentialEnclosingClass = syntaxNode.Parent;

        while (potentialEnclosingClass != null && potentialEnclosingClass is not ClassDeclarationSyntax)
        {
            potentialEnclosingClass = potentialEnclosingClass.Parent;
        }

        return potentialEnclosingClass as ClassDeclarationSyntax;
    }

    private void ReportFieldTypeDiagnostic(
        SyntaxNodeAnalysisContext context,
        MemberAccessExpressionSyntax memberAccessExpressionSyntax,
        DiagnosticDescriptor diagnosticDescriptor)
    {
        var location = memberAccessExpressionSyntax.GetMethodInvocationLocation();
        var diagnostic = Diagnostic.Create(diagnosticDescriptor, location);

        context.ReportDiagnostic(diagnostic);
    }
}
