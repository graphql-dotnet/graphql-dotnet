using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Models;

/// <summary>
/// Immutable record containing all AOT attribute data extracted from a candidate schema class.
/// </summary>
public readonly record struct SchemaAttributeData(
    INamedTypeSymbol SchemaClass,
    ImmutableArray<RootTypeInfo> QueryTypes,
    ImmutableArray<RootTypeInfo> MutationTypes,
    ImmutableArray<RootTypeInfo> SubscriptionTypes,
    ImmutableArray<ITypeSymbol> OutputTypes,
    ImmutableArray<ITypeSymbol> InputTypes,
    ImmutableArray<ITypeSymbol> GraphTypes,
    ImmutableArray<TypeMappingInfo> TypeMappings,
    ImmutableArray<ITypeSymbol> ListTypes,
    ImmutableArray<TypeMappingInfo> RemapTypes);

/// <summary>
/// Represents a single-argument AOT attribute with its generic type parameter.
/// </summary>
public readonly record struct RootTypeInfo(
    ITypeSymbol TypeArgument,
    bool IsClrType);

/// <summary>
/// Represents a two-argument AOT attribute (TypeMapping or RemapType) with both generic type parameters.
/// </summary>
public readonly record struct TypeMappingInfo(
    ITypeSymbol FromType,
    ITypeSymbol ToType);
