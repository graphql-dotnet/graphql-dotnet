using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace GraphQL.SourceGenerators.Providers;

/// <summary>
/// Provider D: Identifies candidate class declarations that have AOT-related attributes.
/// Uses efficient syntax-based filtering combined with semantic attribute checking.
/// </summary>
internal static class SyntaxProvider
{
    /// <summary>
    /// Creates an incremental provider that identifies all partial class declarations
    /// decorated with AOT-related attributes.
    /// </summary>
    /// <remarks>
    /// This implementation uses a three-stage approach:
    /// 1. Resolve AOT attribute symbols once per compilation (cached)
    /// 2. Syntax predicate: Fast check for partial classes with attributes
    /// 3. Semantic transform: Verifies the presence of AOT attributes using cached symbols
    /// </remarks>
    public static IncrementalValuesProvider<ClassDeclarationSyntax> CreateCandidateProvider(
        IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Resolve attribute symbols once per compilation (incremental caching)
        var aotAttributeSymbols = context.CompilationProvider
            .Select(static (compilation, _) => GetAotAttributeSymbols(compilation));

        // Step 2: Create syntax provider for candidate classes
        var candidateClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsPartialClassWithAttributes(node),
                transform: static (context, _) => (context.SemanticModel, (ClassDeclarationSyntax)context.Node));

        // Step 3: Combine resolved symbols with candidates and filter
        return candidateClasses
            .Combine(aotAttributeSymbols)
            .Where(static tuple =>
            {
                var ((semanticModel, classDeclaration), attributeSymbols) = tuple;
                return HasAotAttribute(semanticModel, classDeclaration, attributeSymbols);
            })
            .Select(static (tuple, _) => tuple.Left.Item2);
    }

    /// <summary>
    /// Fast syntax-only check to determine if a node is a partial class with attributes.
    /// </summary>
    private static bool IsPartialClassWithAttributes(SyntaxNode node)
    {
        // Must be a class declaration
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Must have the partial modifier (AOT schemas must be partial for code generation)
        if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            return false;

        // Must have at least one attribute list
        return classDecl.AttributeLists.Count > 0;
    }

    /// <summary>
    /// Resolves AOT attribute symbols from the compilation once and caches them.
    /// Called once per compilation by the incremental generator.
    /// </summary>
    private static ImmutableArray<INamedTypeSymbol?> GetAotAttributeSymbols(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol?>(Constants.AttributeNames.All.Length);

        foreach (var attributeName in Constants.AttributeNames.All)
        {
            // Resolve each attribute symbol using metadata name
            // Returns null if the attribute type is not referenced by the compilation
            var symbol = compilation.GetTypeByMetadataName(attributeName);
            builder.Add(symbol);
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Checks if the class has any AOT attributes using pre-resolved attribute symbols.
    /// Uses symbol equality comparison which is more robust than string matching.
    /// Handles both generic and non-generic attributes correctly.
    /// </summary>
    private static bool HasAotAttribute(
        SemanticModel semanticModel,
        ClassDeclarationSyntax classDeclaration,
        ImmutableArray<INamedTypeSymbol?> aotAttributeSymbols)
    {
        // Get the declared symbol for the class
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol == null)
            return false;

        // Check if any attribute on the class matches our pre-resolved AOT attribute symbols
        foreach (var attribute in classSymbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
                continue;

            // For generic attributes (e.g., AotQueryType<T>), we need to compare the unbound generic type
            // OriginalDefinition gives us the unbound generic type definition for comparison
            var attributeDefinition = attributeClass.OriginalDefinition;

            // Use symbol equality comparison with cached symbols
            foreach (var aotAttributeSymbol in aotAttributeSymbols)
            {
                if (aotAttributeSymbol != null &&
                    SymbolEqualityComparer.Default.Equals(attributeDefinition, aotAttributeSymbol))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
