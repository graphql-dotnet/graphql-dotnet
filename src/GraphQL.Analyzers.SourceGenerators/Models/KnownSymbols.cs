using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Models;

/// <summary>
/// Holds resolved INamedTypeSymbol for each AOT attribute type and key interface types.
/// </summary>
public record class KnownSymbols
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
    public INamedTypeSymbol? DoNotMapClrTypeAttribute { get; init; }
    public INamedTypeSymbol? ClrTypeMappingAttribute { get; init; }
    public INamedTypeSymbol? MemberScanAttribute { get; init; }
    public INamedTypeSymbol? ParameterAttribute { get; init; }
    public INamedTypeSymbol? GraphQLConstructorAttribute { get; init; }
    public INamedTypeSymbol? InstanceSourceAttribute { get; init; }
    public INamedTypeSymbol? InputTypeAttributeT { get; init; }
    public INamedTypeSymbol? InputTypeAttribute { get; init; }
    public INamedTypeSymbol? InputBaseTypeAttributeT { get; init; }
    public INamedTypeSymbol? InputBaseTypeAttribute { get; init; }
    public INamedTypeSymbol? OutputTypeAttributeT { get; init; }
    public INamedTypeSymbol? OutputTypeAttribute { get; init; }
    public INamedTypeSymbol? OutputBaseTypeAttributeT { get; init; }
    public INamedTypeSymbol? OutputBaseTypeAttribute { get; init; }
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
    public INamedTypeSymbol? Task { get; init; }
    public INamedTypeSymbol? TaskT { get; init; }
    public INamedTypeSymbol? ValueTaskT { get; init; }
    public INamedTypeSymbol? IDataLoaderResultT { get; init; }
    public INamedTypeSymbol? IObservableT { get; init; }
    public INamedTypeSymbol? IAsyncEnumerableT { get; init; }
    public INamedTypeSymbol? IResolveFieldContext { get; init; }
    public INamedTypeSymbol? CancellationToken { get; init; }
    public INamedTypeSymbol? IInputObjectGraphType { get; init; }
    public INamedTypeSymbol? IObjectGraphType { get; init; }
    public INamedTypeSymbol? IInterfaceGraphType { get; init; }
    public INamedTypeSymbol? ScalarGraphType { get; init; }
    public INamedTypeSymbol? ComplexGraphType { get; init; }
    public INamedTypeSymbol? EnumerationGraphType { get; init; }
    public INamedTypeSymbol? AutoRegisteringObjectGraphType { get; init; }
    public INamedTypeSymbol? AutoRegisteringInputObjectGraphType { get; init; }
    public INamedTypeSymbol? AutoRegisteringInterfaceGraphType { get; init; }

    /// <summary>
    /// Built-in scalar type mappings from CLR types to GraphTypes, matching BuiltInScalarMappingProvider.
    /// Each tuple contains (CLR type symbol, GraphType symbol).
    /// </summary>
    public ImmutableArray<(INamedTypeSymbol ClrType, INamedTypeSymbol GraphType)> BuiltInScalarMappings { get; init; }

    /// <summary>
    /// Built-in scalar GraphType symbols.
    /// </summary>
    public ImmutableArray<INamedTypeSymbol> BuiltInScalars { get; init; }
}
