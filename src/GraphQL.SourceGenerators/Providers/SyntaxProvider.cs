using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace GraphQL.SourceGenerators.Providers;

/// <summary>
/// Provider D: Identifies candidate class declarations that have AOT-related attributes.
/// Uses the efficient ForAttributeWithMetadataName API for optimal incremental compilation performance.
/// </summary>
internal static class SyntaxProvider
{
    /// <summary>
    /// Creates an incremental provider that identifies all partial class declarations
    /// decorated with AOT-related attributes.
    /// </summary>
    /// <remarks>
    /// This implementation uses ForAttributeWithMetadataName which is highly optimized:
    /// - Roslyn maintains an internal index of symbols by attribute
    /// - Only processes syntax nodes that actually have the target attribute
    /// - Provides both syntax and semantic information in one pass
    /// - Automatically handles incremental compilation caching
    /// 
    /// Returns INamedTypeSymbol for semantic analysis, deduplicated using symbol equality.
    /// </remarks>
    public static IncrementalValuesProvider<INamedTypeSymbol> CreateCandidateProvider(
        IncrementalGeneratorInitializationContext context)
    {
        // Create a collected provider for each AOT attribute type
        var collectedProviders =
            new IncrementalValueProvider<ImmutableArray<INamedTypeSymbol>>[Constants.AttributeNames.All.Length];

        for (int i = 0; i < Constants.AttributeNames.All.Length; i++)
            collectedProviders[i] = CreateProviderForAttribute(context, Constants.AttributeNames.All[i]).Collect();

        // Combine all collected providers into a single array provider
        var combined = collectedProviders[0];
        for (int i = 1; i < collectedProviders.Length; i++)
        {
            var current = collectedProviders[i];
            combined = combined.Combine(current)
                .Select(static (t, _) => t.Left.AddRange(t.Right));
        }

        // Deduplicate
        return combined
            .SelectMany(static (syms, _) => syms.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default));
    }

    /// <summary>
    /// Creates a provider for a specific AOT attribute using ForAttributeWithMetadataName.
    /// </summary>
    private static IncrementalValuesProvider<INamedTypeSymbol> CreateProviderForAttribute(
        IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName,
            predicate: static (node, _) => IsPartialClass(node),
            transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);
    }

    /// <summary>
    /// Fast syntax-only check to determine if a node is a partial class.
    /// </summary>
    /// <remarks>
    /// ForAttributeWithMetadataName already filters by attribute presence,
    /// so we only need to verify the class is partial (required for code generation).
    /// </remarks>
    private static bool IsPartialClass(SyntaxNode node)
    {
        // Must be a class declaration with the partial modifier
        return node is ClassDeclarationSyntax classDecl &&
               classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }
}
