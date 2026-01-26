using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Models;

/// <summary>
/// Immutable record containing discovered type information from scanning a CLR input type.
/// </summary>
public readonly record struct TypeScanResult(
    ITypeSymbol ScannedType,
    ImmutableArray<ITypeSymbol> DiscoveredInputClrTypes,
    ImmutableArray<ITypeSymbol> DiscoveredGraphTypes,
    ImmutableArray<ITypeSymbol> InputListTypes);
