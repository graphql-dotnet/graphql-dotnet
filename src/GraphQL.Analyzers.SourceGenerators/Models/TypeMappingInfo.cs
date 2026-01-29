using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Models;

/// <summary>
/// Represents a two-argument AOT attribute (TypeMapping or RemapType) with both generic type parameters.
/// </summary>
public readonly record struct TypeMappingInfo(
    ITypeSymbol FromType,
    ITypeSymbol ToType);
