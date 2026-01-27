using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Models;

/// <summary>
/// Represents an AotOutputType attribute with its generic type parameter and optional IsInterface property.
/// </summary>
public readonly record struct OutputTypeInfo(
    ITypeSymbol TypeArgument,
    bool? IsInterface);
