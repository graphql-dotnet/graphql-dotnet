using GraphQL.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Providers;

/// <summary>
/// Provides a way to resolve attribute symbols for AOT compilation.
/// </summary>
public static class KnownSymbolsProvider
{
    /// <summary>
    /// Creates a provider that resolves INamedTypeSymbol for all AOT attribute types.
    /// </summary>
    public static IncrementalValueProvider<KnownSymbols> CreateAttributeSymbolsProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider.Select(static (compilation, _) =>
        {
            return new KnownSymbols
            {
                AotQueryType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_QUERY_TYPE),
                AotMutationType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_MUTATION_TYPE),
                AotSubscriptionType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_SUBSCRIPTION_TYPE),
                AotOutputType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_OUTPUT_TYPE),
                AotInputType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_INPUT_TYPE),
                AotGraphType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_GRAPH_TYPE),
                AotTypeMapping = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_TYPE_MAPPING),
                AotListType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_LIST_TYPE),
                AotRemapType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_REMAP_TYPE),
                IGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.IGRAPH_TYPE),
                NonNullGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.NON_NULL_GRAPH_TYPE),
                ListGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.LIST_GRAPH_TYPE),
                GraphQLClrInputTypeReference = compilation.GetTypeByMetadataName(Constants.TypeNames.GRAPHQL_CLR_INPUT_TYPE_REFERENCE),
                GraphQLClrOutputTypeReference = compilation.GetTypeByMetadataName(Constants.TypeNames.GRAPHQL_CLR_OUTPUT_TYPE_REFERENCE),
                IgnoreAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.IGNORE),
                InputTypeAttributeT = compilation.GetTypeByMetadataName(Constants.AttributeNames.INPUT_TYPE),
                InputBaseTypeAttributeT = compilation.GetTypeByMetadataName(Constants.AttributeNames.INPUT_BASE_TYPE),
                BaseGraphTypeAttributeT = compilation.GetTypeByMetadataName(Constants.AttributeNames.BASE_GRAPH_TYPE),
                IEnumerableT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1"),
                IListT = compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1"),
                ListT = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1"),
                ICollectionT = compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1"),
                IReadOnlyCollectionT = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1"),
                IReadOnlyListT = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1"),
                HashSetT = compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1"),
                ISetT = compilation.GetTypeByMetadataName("System.Collections.Generic.ISet`1"),
            };
        });
    }
}
