using System.Collections.Immutable;
using GraphQL.Analyzers.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.SourceGenerators.Providers;

/// <summary>
/// Identifies candidate class declarations that have AOT-related attributes.
/// Uses the efficient ForAttributeWithMetadataName API for optimal incremental compilation performance.
/// </summary>
public static class CandidateProvider
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
    /// Returns CandidateClass containing both syntax and semantic model for code generation.
    /// </remarks>
    public static IncrementalValuesProvider<CandidateClass> CreateCandidateProvider(
        IncrementalGeneratorInitializationContext context)
    {
        // Create a provider for the AotSchema symbol to use in filtering
        var aotSchemaSymbolProvider = context.CompilationProvider
            .Select(static (compilation, _) => compilation.GetTypeByMetadataName(Constants.TypeNames.AOT_SCHEMA));

        // Create a collected provider for each AOT attribute type
        var collectedProviders =
            new IncrementalValueProvider<ImmutableArray<CandidateClass>>[Constants.AttributeNames.AllAot.Length];

        for (int i = 0; i < Constants.AttributeNames.AllAot.Length; i++)
            collectedProviders[i] = CreateProviderForAttribute(context, Constants.AttributeNames.AllAot[i]).Collect();

        // Combine all collected providers into a single array provider
        var combined = collectedProviders[0];
        for (int i = 1; i < collectedProviders.Length; i++)
        {
            var current = collectedProviders[i];
            combined = combined.Combine(current)
                .Select(static (t, _) => t.Left.AddRange(t.Right));
        }

        // Combine with AotSchema symbol and deduplicate/filter
        return combined
            .Combine(aotSchemaSymbolProvider)
            .SelectMany(static (tuple, _) => DeduplicateAndFilter(tuple.Left, tuple.Right));
    }

    /// <summary>
    /// Creates a provider for a specific AOT attribute using ForAttributeWithMetadataName.
    /// </summary>
    private static IncrementalValuesProvider<CandidateClass> CreateProviderForAttribute(
        IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName,
            predicate: static (node, _) => IsPartialClass(node),
            transform: static (ctx, _) => new CandidateClass(
                (ClassDeclarationSyntax)ctx.TargetNode,
                ctx.SemanticModel,
                null!));
    }

    /// <summary>
    /// Fast syntax-only check to determine if a node is a partial class.
    /// </summary>
    /// <remarks>
    /// ForAttributeWithMetadataName already filters by attribute presence,
    /// so we only need to verify the class is partial (required for code generation).
    /// Additionally, all containing classes must also be partial.
    /// </remarks>
    private static bool IsPartialClass(SyntaxNode? node)
    {
        // Must be a class declaration with the partial modifier
        if (node is not ClassDeclarationSyntax)
            return false;

        // Check that all containing classes are also partial
        while (node is ClassDeclarationSyntax classDecl)
        {
            if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return false;

            node = classDecl.Parent;
        }

        return true;
    }

    /// <summary>
    /// Manually deduplicate candidates by class symbol and filter to only include
    /// classes that inherit from GraphQL.Types.AotSchema.
    /// For partial classes with attributes on multiple declarations, uses the first declaration.
    /// </summary>
    private static ImmutableArray<CandidateClass> DeduplicateAndFilter(
        ImmutableArray<CandidateClass> candidates,
        INamedTypeSymbol? aotSchemaSymbol)
    {
        if (candidates.Length == 0)
            return candidates;

        // Group by class symbol to handle partial classes correctly
        var seenSymbols = new Dictionary<INamedTypeSymbol, CandidateClass>(SymbolEqualityComparer.Default);

        for (int i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            var classSymbol = candidate.SemanticModel.GetDeclaredSymbol(candidate.ClassDeclarationSyntax);

            if (classSymbol == null)
                continue;

            // If we have seen it, we have multiple partial declarations with attributes
            // Keep the first one we found (it doesn't matter which we use as they represent the same class)
            if (seenSymbols.ContainsKey(classSymbol))
                continue;

            // If we haven't seen this symbol before, add it
            if (InheritsFromAotSchema(classSymbol, aotSchemaSymbol))
            {
                // Create a new candidate with the ClassSymbol populated
                var candidateWithSymbol = new CandidateClass(
                    candidate.ClassDeclarationSyntax,
                    candidate.SemanticModel,
                    classSymbol);
                seenSymbols.Add(classSymbol, candidateWithSymbol);
            }
        }

        return seenSymbols.Values.ToImmutableArray();
    }

    /// <summary>
    /// Checks if a class symbol inherits from GraphQL.Types.AotSchema (directly or indirectly).
    /// </summary>
    private static bool InheritsFromAotSchema(INamedTypeSymbol classSymbol, INamedTypeSymbol? aotSchemaSymbol)
    {
        if (aotSchemaSymbol == null)
            return false;

        // Walk up the inheritance chain to check if any base type is the AotSchema symbol
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, aotSchemaSymbol))
                return true;
            baseType = baseType.BaseType;
        }

        return false;
    }
}
