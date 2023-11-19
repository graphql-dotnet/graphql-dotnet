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
        // MyInput<SourceType> : InputObjectGraphType<SourceType>
        if (sourceTypeSymbol is ITypeParameterSymbol)
        {
            return;
        }

        if (sourceTypeSymbol.IsAbstract)
        {
            ReportConstructorDiagnostic();
            return;
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
            ReportConstructorDiagnostic();
            return;
        }

        // single public or implicit default constructor
        if (constructors.Count == 1)
        {
            return;
        }

        // explicit default constructor
        if (constructors.Any(ctor => ctor.Parameters.Length == 0))
        {
            return;
        }

        // one constructor with [GraphQLConstructor] attribute
        if (constructors.Count(HasGraphQLAttribute) == 1)
        {
            return;
        }

        // no default constructors and (no [GraphQLConstructor] attribute, or multiple [GraphQLConstructor] attributes)
        ReportConstructorDiagnostic();
        return;

        void ReportConstructorDiagnostic()
        {
            var location = FindSourceSymbolUsageLocation();
            if (location != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    CanNotResolveInputSourceTypeConstructor,
                    location,
                    (sourceTypeSymbol as INamedTypeSymbol)?.Name));
            }
        }

        // the location of the 'SourceType' in InputObjectGraphType<SourceType>
        Location? FindSourceSymbolUsageLocation() =>
            inputObjectDeclarationSyntax.BaseList
                ?.Types
                .Select(baseTypeSyntax => baseTypeSyntax.Type)
                .OfType<GenericNameSyntax>()
                .SelectMany(generic => generic.TypeArgumentList.Arguments)
                .FirstOrDefault(arg =>
                {
                    var argType = context.SemanticModel.GetTypeInfo(arg).Type;
                    return argType != null && SymbolEqualityComparer.Default.Equals(argType, sourceTypeSymbol);
                })
                ?.GetLocation();
    }

    private static bool HasGraphQLAttribute(IMethodSymbol constructor) =>
        constructor
            .GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == Constants.Types.GraphQLConstructorAttribute &&
                         attr.AttributeClass.IsGraphQLSymbol());
}
