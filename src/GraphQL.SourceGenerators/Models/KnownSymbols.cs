using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Models;

/// <summary>
/// Holds resolved INamedTypeSymbol for each AOT attribute type and key interface types.
/// </summary>
public readonly record struct KnownSymbols
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
    public INamedTypeSymbol? GraphQLClrOutputTypeReference { get; init; }
    public INamedTypeSymbol? IgnoreAttribute { get; init; }
    public INamedTypeSymbol? InputTypeAttributeT { get; init; }
    public INamedTypeSymbol? InputTypeAttribute { get; init; }
    public INamedTypeSymbol? InputBaseTypeAttributeT { get; init; }
    public INamedTypeSymbol? InputBaseTypeAttribute { get; init; }
    public INamedTypeSymbol? BaseGraphTypeAttributeT { get; init; }
    public INamedTypeSymbol? BaseGraphTypeAttribute { get; init; }
    public INamedTypeSymbol? IEnumerableT { get; init; }
    public INamedTypeSymbol? IListT { get; init; }
    public INamedTypeSymbol? ListT { get; init; }
    public INamedTypeSymbol? ICollectionT { get; init; }
    public INamedTypeSymbol? IReadOnlyCollectionT { get; init; }
    public INamedTypeSymbol? IReadOnlyListT { get; init; }
    public INamedTypeSymbol? HashSetT { get; init; }
    public INamedTypeSymbol? ISetT { get; init; }
}
