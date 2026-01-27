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
                MemberScanAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.MEMBER_SCAN),
                ParameterAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.PARAMETER_ATTRIBUTE),
                InputTypeAttributeT = compilation.GetTypeByMetadataName(Constants.AttributeNames.INPUT_TYPE),
                InputTypeAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.INPUT_TYPE_NON_GENERIC),
                InputBaseTypeAttributeT = compilation.GetTypeByMetadataName(Constants.AttributeNames.INPUT_BASE_TYPE),
                InputBaseTypeAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.INPUT_BASE_TYPE_NON_GENERIC),
                OutputTypeAttributeT = compilation.GetTypeByMetadataName(Constants.AttributeNames.OUTPUT_TYPE),
                OutputTypeAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.OUTPUT_TYPE_NON_GENERIC),
                OutputBaseTypeAttributeT = compilation.GetTypeByMetadataName(Constants.AttributeNames.OUTPUT_BASE_TYPE),
                OutputBaseTypeAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.OUTPUT_BASE_TYPE_NON_GENERIC),
                BaseGraphTypeAttributeT = compilation.GetTypeByMetadataName(Constants.AttributeNames.BASE_GRAPH_TYPE),
                BaseGraphTypeAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.BASE_GRAPH_TYPE_NON_GENERIC),
                IEnumerableT = compilation.GetTypeByMetadataName(Constants.TypeNames.IENUMERABLE_T),
                IListT = compilation.GetTypeByMetadataName(Constants.TypeNames.ILIST_T),
                ListT = compilation.GetTypeByMetadataName(Constants.TypeNames.LIST_T),
                ICollectionT = compilation.GetTypeByMetadataName(Constants.TypeNames.ICOLLECTION_T),
                IReadOnlyCollectionT = compilation.GetTypeByMetadataName(Constants.TypeNames.IREADONLY_COLLECTION_T),
                IReadOnlyListT = compilation.GetTypeByMetadataName(Constants.TypeNames.IREADONLY_LIST_T),
                HashSetT = compilation.GetTypeByMetadataName(Constants.TypeNames.HASHSET_T),
                ISetT = compilation.GetTypeByMetadataName(Constants.TypeNames.ISET_T),
                Task = compilation.GetTypeByMetadataName(Constants.TypeNames.TASK),
                TaskT = compilation.GetTypeByMetadataName(Constants.TypeNames.TASK_T),
                ValueTaskT = compilation.GetTypeByMetadataName(Constants.TypeNames.VALUE_TASK_T),
                IDataLoaderResultT = compilation.GetTypeByMetadataName(Constants.TypeNames.IDATA_LOADER_RESULT_T),
                IResolveFieldContext = compilation.GetTypeByMetadataName(Constants.TypeNames.IRESOLVE_FIELD_CONTEXT),
                CancellationToken = compilation.GetTypeByMetadataName(Constants.TypeNames.CANCELLATION_TOKEN),
                IInputObjectGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.IINPUT_OBJECT_GRAPH_TYPE),
                IObjectGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.IOBJECT_GRAPH_TYPE),
                IInterfaceGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.IINTERFACE_GRAPH_TYPE),
                ScalarGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.SCALAR_GRAPH_TYPE),
            };
        });
    }
}
