using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NotAGraphTypeAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MustNotBeConvertibleToGraphType = new(
        id: DiagnosticIds.MUST_NOT_BE_CONVERTIBLE_TO_GRAPH_TYPE,
        title: "The type must not be convertible to IGraphType",
        messageFormat: "The type '{0}' cannot be used as type parameter '{1}' in the generic type or method '{2}'. " +
                       "The type '{0}' must NOT be convertible to 'IGraphType'.",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.MUST_NOT_BE_CONVERTIBLE_TO_GRAPH_TYPE);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(MustNotBeConvertibleToGraphType);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeGenericName, SyntaxKind.GenericName);
    }

    private void AnalyzeGenericName(SyntaxNodeAnalysisContext context)
    {
        var genericName = (GenericNameSyntax)context.Node;
        var genericNameSymbol = context.SemanticModel.GetSymbolInfo(genericName).Symbol;

        if (genericNameSymbol == null)
        {
            return;
        }

        if (!genericNameSymbol.IsGraphQLSymbol())
        {
            return;
        }

        ImmutableArray<ITypeParameterSymbol> typeParameters;
        ImmutableArray<ITypeSymbol> typeArguments;

        switch (genericNameSymbol)
        {
            case INamedTypeSymbol namedTypeSymbol:
                typeParameters = namedTypeSymbol.TypeParameters;
                typeArguments = namedTypeSymbol.TypeArguments;
                break;
            case IMethodSymbol methodSymbol:
                typeParameters = methodSymbol.TypeParameters;
                typeArguments = methodSymbol.TypeArguments;
                break;
            default:
                return;
        }

        var notAGraphTypeAttribute = context.Compilation.GetTypeByMetadataName(Constants.MetadataNames.NotAGraphTypeAttribute);
        if (notAGraphTypeAttribute == null)
        {
            return;
        }

        var graphTypeInterface = context.Compilation.GetTypeByMetadataName(Constants.MetadataNames.IGraphType);
        if (graphTypeInterface == null)
        {
            return;
        }

        string? genericNameString = null;

        for (int i = 0; i < typeParameters.Length; i++)
        {
            var typeParam = typeParameters[i];
            var typeArg = typeArguments[i];

            if (HasNotAGraphTypeAttribute(typeParam) && IsGraphType(typeArg))
            {
                var location = genericName.TypeArgumentList.Arguments[i].GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(
                    MustNotBeConvertibleToGraphType,
                    location,
                    typeArg.Name,
                    typeParam.Name,
                    genericNameString ??= GetGenericNameString(typeParameters, genericName)));
            }
        }

        return;

        bool HasNotAGraphTypeAttribute(ISymbol typeParam)
        {
            return typeParam.GetAttributes()
                .Any(data => SymbolEqualityComparer.Default.Equals(data.AttributeClass, notAGraphTypeAttribute));
        }

        bool IsGraphType(ITypeSymbol typeArg)
        {
            return typeArg.AllInterfaces
                .Any(@interface => SymbolEqualityComparer.Default.Equals(@interface, graphTypeInterface));
        }

        string GetGenericNameString(ImmutableArray<ITypeParameterSymbol> typeParams, SimpleNameSyntax name)
        {
            string argList = string.Join(", ", typeParams.Select(p => p.Name));
            return $"{name.Identifier.Text}<{argList}>";
        }
    }
}
