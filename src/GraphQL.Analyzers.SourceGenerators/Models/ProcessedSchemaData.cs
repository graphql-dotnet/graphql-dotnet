using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Models;

/// <summary>
/// Immutable record containing processed schema data after walking all discovered types.
/// This includes all GraphTypes discovered through attributes and type graph traversal,
/// as well as mappings from CLR types to their corresponding wrapped GraphTypes.
/// </summary>
public readonly record struct ProcessedSchemaData(
    INamedTypeSymbol SchemaClass,
    ITypeSymbol? QueryRootGraphType,
    ITypeSymbol? MutationRootGraphType,
    ITypeSymbol? SubscriptionRootGraphType,
    ImmutableEquatableArray<ISymbol> DiscoveredGraphTypes,
    ImmutableEquatableArray<(ISymbol ClrType, ISymbol GraphType)> OutputClrTypeMappings,
    ImmutableEquatableArray<(ISymbol ClrType, ISymbol GraphType)> InputClrTypeMappings,
    ImmutableEquatableArray<ISymbol> InputListTypes,
    ImmutableEquatableArray<(ISymbol GraphType, ImmutableEquatableArray<ISymbol> Members)> GeneratedGraphTypesWithMembers,
    ImmutableEquatableArray<(ISymbol FromType, ISymbol ToType)> RemapTypes);
