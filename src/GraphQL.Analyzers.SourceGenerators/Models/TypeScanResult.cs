using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Models;

/// <summary>
/// Immutable record containing discovered type information from scanning a CLR input type.
/// </summary>
public readonly record struct TypeScanResult(
    ITypeSymbol ScannedType,
    ImmutableEquatableArray<ISymbol> DiscoveredInputClrTypes,
    ImmutableEquatableArray<ISymbol> DiscoveredOutputClrTypes,
    ImmutableEquatableArray<ISymbol> DiscoveredGraphTypes,
    ImmutableEquatableArray<ISymbol> InputListTypes,
    ImmutableEquatableArray<ISymbol> SelectedMembers);
