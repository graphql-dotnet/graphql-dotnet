using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
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

    private static void AnalyzeSourceTypeConstructors(
        SyntaxNodeAnalysisContext context,
        BaseTypeDeclarationSyntax inputObjectDeclarationSyntax,
        ITypeSymbol sourceTypeSymbol)
    {
        if (!AnalyzeSourceTypeConstructors(sourceTypeSymbol))
        {
            var location = FindSourceSymbolUsageLocation(context, inputObjectDeclarationSyntax, sourceTypeSymbol);
            ReportConstructorDiagnostic(context, sourceTypeSymbol, location);
        }
    }

    private static void AnalyzeSourceTypeConstructors(
        SyntaxNodeAnalysisContext context,
        GenericNameSyntax autoRegisteringInputGenericName,
        ITypeSymbol sourceTypeSymbol)
    {
        if (!AnalyzeSourceTypeConstructors(sourceTypeSymbol))
        {
            var location = FindSourceSymbolUsageLocation(context, autoRegisteringInputGenericName, sourceTypeSymbol);
            ReportConstructorDiagnostic(context, sourceTypeSymbol, location);
        }
    }

    private static bool AnalyzeSourceTypeConstructors(ITypeSymbol sourceTypeSymbol)
    {
        // MyInput<SourceType> : InputObjectGraphType<SourceType>
        if (sourceTypeSymbol is ITypeParameterSymbol)
        {
            return true;
        }

        if (sourceTypeSymbol.IsAbstract)
        {
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

        // no public constructor
        if (constructors.Count == 0)
        {
            return false;
        }

        // single public or implicit default constructor
        if (constructors.Count == 1)
        {
            return true;
        }

        // explicit default constructor
        if (constructors.Any(ctor => ctor.Parameters.Length == 0))
        {
            return true;
        }

        // one constructor with [GraphQLConstructor] attribute
        if (constructors.Count(HasGraphQLAttribute) == 1)
        {
            return true;
        }

        // no default constructors and (no [GraphQLConstructor] attribute, or multiple [GraphQLConstructor] attributes)
        return false;
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
                var argType = context.SemanticModel.GetTypeInfo(arg).Type;
                return argType != null && SymbolEqualityComparer.Default.Equals(argType, sourceTypeSymbol);
            })
            ?.GetLocation();

    private static bool HasGraphQLAttribute(IMethodSymbol constructor) =>
        constructor
            .GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == Constants.Types.GraphQLConstructorAttribute &&
                         attr.AttributeClass.IsGraphQLSymbol());
}
