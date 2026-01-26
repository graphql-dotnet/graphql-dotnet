using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Models;

/// <summary>
/// Holds resolved INamedTypeSymbol for each AOT attribute type and key interface types.
/// </summary>
public readonly struct KnownSymbols
{
    public INamedTypeSymbol? AotQueryType { get; init; }
    public INamedTypeSymbol? AotMutationType { get; init; }
    public INamedTypeSymbol? AotSubscriptionType { get; init; }
    public INamedTypeSymbol? AotOutputType { get; init; }
    public INamedTypeSymbol? AotInputType { get; init; }
    public INamedTypeSymbol? AotGraphType { get; init; }
    public INamedTypeSymbol? AotTypeMapping { get; init; }
    public INamedTypeSymbol? AotListType { get; init; }
    public INamedTypeSymbol? AotRemapType { get; init; }
    public INamedTypeSymbol? IGraphType { get; init; }
    public INamedTypeSymbol? NonNullGraphType { get; init; }
    public INamedTypeSymbol? ListGraphType { get; init; }
    public INamedTypeSymbol? GraphQLClrInputTypeReference { get; init; }
}
