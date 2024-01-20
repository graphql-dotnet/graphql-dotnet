using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

public partial class InputGraphTypeAnalyzer
{
    public static readonly DiagnosticDescriptor CanNotResolveInputSourceTypeConstructor = new(
        id: DiagnosticIds.CAN_NOT_RESOLVE_INPUT_SOURCE_TYPE_CONSTRUCTOR,
        title: "Can not resolve input source type constructor",
        messageFormat: "The input source type '{0}' must be a non-abstract, have a parameterless constructor, or " +
                       "a singular parameterized constructor, or a parameterized constructor annotated with " +
                       $"'{Constants.Types.GraphQLConstructorAttribute}'",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.CAN_NOT_RESOLVE_INPUT_SOURCE_TYPE_CONSTRUCTOR);

    private static IMethodSymbol? AnalyzeSourceTypeConstructors(
        SyntaxNodeAnalysisContext context,
        BaseTypeDeclarationSyntax inputObjectDeclarationSyntax,
        ITypeSymbol sourceTypeSymbol)
    {
        if (!TryResolveSourceTypeConstructor(context, sourceTypeSymbol, out var ctor))
        {
            var location = FindSourceSymbolUsageLocation(context, inputObjectDeclarationSyntax, sourceTypeSymbol);
            ReportConstructorDiagnostic(context, sourceTypeSymbol, location);
        }

        return ctor;
    }

    private static IMethodSymbol? AnalyzeSourceTypeConstructors(
        SyntaxNodeAnalysisContext context,
        GenericNameSyntax autoRegisteringInputGenericName,
        ITypeSymbol sourceTypeSymbol)
    {
        if (!TryResolveSourceTypeConstructor(context, sourceTypeSymbol, out var ctor))
        {
            var location = FindSourceSymbolUsageLocation(context, autoRegisteringInputGenericName, sourceTypeSymbol);
            ReportConstructorDiagnostic(context, sourceTypeSymbol, location);
        }

        return ctor;
    }

    // Mimic the AutoRegisteringHelper.GetConstructorOrDefault behavior
    private static bool TryResolveSourceTypeConstructor(
        SyntaxNodeAnalysisContext context,
        ITypeSymbol sourceTypeSymbol,
        out IMethodSymbol? ctor)
    {
        // MyInput<SourceType> : InputObjectGraphType<SourceType>
        if (sourceTypeSymbol is ITypeParameterSymbol)
        {
            ctor = null;
            return true;
        }

        if (sourceTypeSymbol.IsAbstract)
        {
            ctor = null;
            return false;
        }

        var constructors = sourceTypeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => method is
            {
                MethodKind: MethodKind.Constructor,
                DeclaredAccessibility: Accessibility.Public
            })
            .ToList();

        // if there are no public constructors, return null
        if (constructors.Count == 0)
        {
            ctor = null;
            return false;
        }

        // if there is only one public constructor, return it
        if (constructors.Count == 1)
        {
            ctor = constructors[0];
            return true;
        }

        var graphQlConstructorAttribute = context.Compilation
            .GetTypeByMetadataName(Constants.MetadataNames.GraphQLConstructorAttribute);

        // if there are multiple public constructors, return the one marked with
        // GraphQLConstructorAttribute, or the parameterless constructor, or null
        IMethodSymbol? match = null;
        IMethodSymbol? parameterless = null;
        foreach (var constructor in constructors)
        {
            if (HasGraphQLAttribute(constructor, graphQlConstructorAttribute))
            {
                // when multiple constructors decorated with [GraphQLConstructor]
                // we ignore all the constructors in this analyzer
                if (match != null)
                {
                    ctor = null;
                    return false;
                }

                match = constructor;
            }

            if (constructor.Parameters.Length == 0)
            {
                parameterless = constructor;
            }
        }

        ctor = match ?? parameterless;
        return ctor != null;
    }

    private static void ReportConstructorDiagnostic(
        SyntaxNodeAnalysisContext context,
        ITypeSymbol sourceTypeSymbol,
        Location? location)
    {
        if (location != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CanNotResolveInputSourceTypeConstructor,
                location,
                (sourceTypeSymbol as INamedTypeSymbol)?.Name));
        }
    }

    private static Location? FindSourceSymbolUsageLocation(
        SyntaxNodeAnalysisContext context,
        BaseTypeDeclarationSyntax inputObjectDeclarationSyntax,
        ITypeSymbol sourceTypeSymbol) =>
        inputObjectDeclarationSyntax.BaseList
            ?.Types
            .Select(baseTypeSyntax => baseTypeSyntax.Type)
            .OfType<GenericNameSyntax>()
            .Select(genericName => FindSourceSymbolUsageLocation(context, genericName, sourceTypeSymbol))
            .FirstOrDefault(location => location != null);

    // the location of the 'SourceType' in InputObjectGraphType<SourceType>
    private static Location? FindSourceSymbolUsageLocation(
        SyntaxNodeAnalysisContext context,
        GenericNameSyntax genericName,
        ITypeSymbol sourceTypeSymbol) =>
        genericName
            .TypeArgumentList.Arguments
            .FirstOrDefault(arg =>
            {
                var argType = context.SemanticModel.GetTypeInfo(arg, context.CancellationToken).Type;
                return argType != null && SymbolEqualityComparer.Default.Equals(argType, sourceTypeSymbol);
            })
            ?.GetLocation();

    private static bool HasGraphQLAttribute(ISymbol constructor, ISymbol? graphQlConstructorAttribute) =>
        constructor
            .GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, graphQlConstructorAttribute));
}
