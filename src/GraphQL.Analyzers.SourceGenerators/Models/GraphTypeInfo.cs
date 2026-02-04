using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Models;

/// <summary>
/// Represents an AotGraphType attribute with its generic type parameter and AutoRegisterClrMapping property.
/// </summary>
public readonly record struct GraphTypeInfo(
    ITypeSymbol TypeArgument,
    bool AutoRegisterClrMapping);
