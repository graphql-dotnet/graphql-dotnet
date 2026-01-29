using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Models;

/// <summary>
/// Immutable record containing all AOT attribute data extracted from a candidate schema class.
/// </summary>
public readonly record struct SchemaAttributeData(
    INamedTypeSymbol SchemaClass,
    RootTypeInfo? QueryType,
    RootTypeInfo? MutationType,
    RootTypeInfo? SubscriptionType,
    ImmutableArray<OutputTypeInfo> OutputTypes,
    ImmutableArray<ITypeSymbol> InputTypes,
    ImmutableArray<GraphTypeInfo> GraphTypes,
    ImmutableArray<TypeMappingInfo> TypeMappings,
    ImmutableArray<ITypeSymbol> ListTypes,
    ImmutableArray<TypeMappingInfo> RemapTypes);
