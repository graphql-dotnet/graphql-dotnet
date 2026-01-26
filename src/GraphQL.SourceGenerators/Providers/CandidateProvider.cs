using GraphQL.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace GraphQL.SourceGenerators.Providers;

/// <summary>
/// Provider D: Identifies candidate class declarations that have AOT-related attributes.
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
        // Create a collected provider for each AOT attribute type
        var collectedProviders =
            new IncrementalValueProvider<ImmutableArray<CandidateClass>>[Constants.AttributeNames.All.Length];

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

        // Deduplicate by comparing the syntax nodes (using reference equality is fine for syntax nodes)
        // Then filter to only include classes that inherit from GraphQL.Types.AotSchema
        return combined
            .SelectMany(static (candidates, _) => DeduplicateAndFilter(candidates));
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
                ctx.SemanticModel));
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
    /// Manually deduplicate candidates by syntax node reference and filter to only include
    /// classes that inherit from GraphQL.Types.AotSchema.
    /// </summary>
    private static ImmutableArray<CandidateClass> DeduplicateAndFilter(ImmutableArray<CandidateClass> candidates)
    {
        if (candidates.Length == 0)
            return candidates;

        var seen = new HashSet<ClassDeclarationSyntax>();
        var builder = ImmutableArray.CreateBuilder<CandidateClass>();

        for (int i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (seen.Add(candidate.ClassDeclarationSyntax) && InheritsFromAotSchema(candidate))
            {
                builder.Add(candidate);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Checks if a candidate class inherits from GraphQL.Types.AotSchema (directly or indirectly).
    /// </summary>
    private static bool InheritsFromAotSchema(CandidateClass candidate)
    {
        var classSymbol = candidate.SemanticModel.GetDeclaredSymbol(candidate.ClassDeclarationSyntax);

        if (classSymbol == null)
            return false;

        // Walk up the inheritance chain to check if any base type is GraphQL.Types.AotSchema
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.ToDisplayString() == "GraphQL.Types.AotSchema")
                return true;
            baseType = baseType.BaseType;
        }

        return false;
    }
}
