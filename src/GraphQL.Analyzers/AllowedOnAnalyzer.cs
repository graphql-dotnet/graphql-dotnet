using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AllowedOnAnalyzer : DiagnosticAnalyzer
{
    // Violation: any of _forbiddenMethodNames used on type that doesn't implement _allowedInterfaces
    // Fixed:     remove the illegal method
    public static readonly DiagnosticDescriptor IllegalMethodUsage = new(
        id: DiagnosticIds.ILLEGAL_METHOD_USAGE,
        title: "Illegal method usage",
        messageFormat: "'{0}' method is only allowed on types implementing {1}",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.ILLEGAL_METHOD_USAGE);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(IllegalMethodUsage);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
    }

    private void AnalyzeMemberAccessExpression(SyntaxNodeAnalysisContext context)
    {
        var memberAccessExpression = (MemberAccessExpressionSyntax)context.Node;
        if (!memberAccessExpression.IsGraphQLSymbol(context.SemanticModel))
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessExpression);

        ImmutableArray<ITypeSymbol>? allowedTypes = null;
        if (symbolInfo.Symbol != null)
        {
            allowedTypes = GetAllowedTypes(symbolInfo.Symbol);
        }
        else if (symbolInfo.CandidateSymbols.Any())
        {
            allowedTypes = symbolInfo.CandidateSymbols
                .SelectMany(symbol => GetAllowedTypes(symbol)?.Select(typeSymbol => typeSymbol))
                .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
                .ToImmutableArray();
        }

        if (allowedTypes == null || allowedTypes.Value.Length == 0)
        {
            return;
        }

        var fieldInvocationExpression = memberAccessExpression.FindMethodInvocationExpression(Constants.MethodNames.Field);

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

        bool implementsAllowedInterfaces = typeSymbol.AllInterfaces.Any(allowedTypes.Value.Contains);

        if (!implementsAllowedInterfaces)
        {
            ReportFieldTypeDiagnostic(context, memberAccessExpression, IllegalMethodUsage, allowedTypes.Value);
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
        DiagnosticDescriptor diagnosticDescriptor,
        ImmutableArray<ITypeSymbol> allowedTypes)
    {
        string typesString = allowedTypes.Length switch
        {
            1 => allowedTypes[0].Name,
            2 => $"{allowedTypes[0].Name} or {allowedTypes[1].Name}",
            _ => $"{string.Join(", ", allowedTypes.Take(allowedTypes.Length - 1).Select(t => t.Name))} or {allowedTypes[^1].Name}"
        };

        var location = memberAccessExpressionSyntax.GetMethodInvocationLocation();
        var diagnostic = Diagnostic.Create(
            diagnosticDescriptor,
            location,
            memberAccessExpressionSyntax.Name.Identifier.Text,
            typesString);

        context.ReportDiagnostic(diagnostic);
    }

    private static ImmutableArray<ITypeSymbol>? GetAllowedTypes(ISymbol symbol) =>
        symbol.GetAttributes()
            .FirstOrDefault(data => data.AttributeClass?.Name == Constants.MetadataNames.AllowedOnAttribute)
            ?.AttributeClass!.TypeArguments
        ?? ImmutableArray<ITypeSymbol>.Empty;
}
