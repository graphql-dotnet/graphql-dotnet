using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Models;

/// <summary>
/// Represents a single-argument AOT attribute with its generic type parameter.
/// </summary>
public readonly record struct RootTypeInfo(
    ITypeSymbol TypeArgument,
    bool IsClrType);
