using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Models;

/// <summary>
/// Immutable record containing all AOT attribute data extracted from a candidate schema class.
/// </summary>
public readonly record struct SchemaAttributeData(
    INamedTypeSymbol SchemaClass,
    RootTypeInfo? QueryType,
    RootTypeInfo? MutationType,
    RootTypeInfo? SubscriptionType,
    ImmutableArray<ITypeSymbol> OutputTypes,
    ImmutableArray<ITypeSymbol> InputTypes,
    ImmutableArray<ITypeSymbol> GraphTypes,
    ImmutableArray<TypeMappingInfo> TypeMappings,
    ImmutableArray<ITypeSymbol> ListTypes,
    ImmutableArray<TypeMappingInfo> RemapTypes);
