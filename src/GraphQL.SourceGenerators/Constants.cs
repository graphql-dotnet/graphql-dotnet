using System.Collections.Immutable;

namespace GraphQL.SourceGenerators;

internal static class Constants
{
    internal static class AttributeNames
    {
        // Generic attributes require the arity marker (`1 for one type parameter)
        // This is required for GetTypeByMetadataName to resolve generic types correctly
        internal const string AOT_QUERY_TYPE = "GraphQL.AotQueryTypeAttribute`1";
        internal const string AOT_MUTATION_TYPE = "GraphQL.AotMutationTypeAttribute`1";
        internal const string AOT_SUBSCRIPTION_TYPE = "GraphQL.AotSubscriptionTypeAttribute`1";
        internal const string AOT_OUTPUT_TYPE = "GraphQL.AotOutputTypeAttribute`1";
        internal const string AOT_INPUT_TYPE = "GraphQL.AotInputTypeAttribute`1";
        internal const string AOT_GRAPH_TYPE = "GraphQL.AotGraphTypeAttribute`1";
        internal const string AOT_TYPE_MAPPING = "GraphQL.AotTypeMappingAttribute`2";  // 2 type parameters
        internal const string AOT_LIST_TYPE = "GraphQL.AotListTypeAttribute`1";
        internal const string AOT_REMAP_TYPE = "GraphQL.AotRemapTypeAttribute`2";  // 2 type parameters
        internal const string IGNORE = "GraphQL.IgnoreAttribute";
        internal const string MEMBER_SCAN = "GraphQL.MemberScanAttribute";
        internal const string PARAMETER_ATTRIBUTE = "GraphQL.ParameterAttribute";
        internal const string INPUT_TYPE = "GraphQL.InputTypeAttribute`1";
        internal const string INPUT_TYPE_NON_GENERIC = "GraphQL.InputTypeAttribute";
        internal const string INPUT_BASE_TYPE = "GraphQL.InputBaseTypeAttribute`1";
        internal const string INPUT_BASE_TYPE_NON_GENERIC = "GraphQL.InputBaseTypeAttribute";
        internal const string OUTPUT_TYPE = "GraphQL.OutputTypeAttribute`1";
        internal const string OUTPUT_TYPE_NON_GENERIC = "GraphQL.OutputTypeAttribute";
        internal const string OUTPUT_BASE_TYPE = "GraphQL.OutputBaseTypeAttribute`1";
        internal const string OUTPUT_BASE_TYPE_NON_GENERIC = "GraphQL.OutputBaseTypeAttribute";
        internal const string BASE_GRAPH_TYPE = "GraphQL.BaseGraphTypeAttribute`1";
        internal const string BASE_GRAPH_TYPE_NON_GENERIC = "GraphQL.BaseGraphTypeAttribute";

        /// <summary>
        /// Includes AotQueryType, AotMutationType, AotSubscriptionType, AotOutputType, AotInputType,
        /// AotGraphType, AotTypeMapping, AotListType, and AotRemapType.
        /// </summary>
        internal static readonly ImmutableArray<string> All = ImmutableArray.Create(
        [
            AOT_QUERY_TYPE,
            AOT_MUTATION_TYPE,
            AOT_SUBSCRIPTION_TYPE,
            AOT_OUTPUT_TYPE,
            AOT_INPUT_TYPE,
            AOT_GRAPH_TYPE,
            AOT_TYPE_MAPPING,
            AOT_LIST_TYPE,
            AOT_REMAP_TYPE
        ]);
    }

    internal static class TypeNames
    {
        internal const string IGRAPH_TYPE = "GraphQL.Types.IGraphType";
        internal const string NON_NULL_GRAPH_TYPE = "GraphQL.Types.NonNullGraphType`1";
        internal const string LIST_GRAPH_TYPE = "GraphQL.Types.ListGraphType`1";
        internal const string GRAPHQL_CLR_INPUT_TYPE_REFERENCE = "GraphQL.Types.GraphQLClrInputTypeReference`1";
        internal const string GRAPHQL_CLR_OUTPUT_TYPE_REFERENCE = "GraphQL.Types.GraphQLClrOutputTypeReference`1";
        internal const string IRESOLVE_FIELD_CONTEXT = "GraphQL.IResolveFieldContext";
        internal const string CANCELLATION_TOKEN = "System.Threading.CancellationToken";
        internal const string IINPUT_OBJECT_GRAPH_TYPE = "GraphQL.Types.IInputObjectGraphType";
        internal const string IOBJECT_GRAPH_TYPE = "GraphQL.Types.IObjectGraphType";
        internal const string IINTERFACE_GRAPH_TYPE = "GraphQL.Types.IInterfaceGraphType";
        internal const string SCALAR_GRAPH_TYPE = "GraphQL.Types.ScalarGraphType";
        internal const string IENUMERABLE_T = "System.Collections.Generic.IEnumerable`1";
        internal const string ILIST_T = "System.Collections.Generic.IList`1";
        internal const string LIST_T = "System.Collections.Generic.List`1";
        internal const string ICOLLECTION_T = "System.Collections.Generic.ICollection`1";
        internal const string IREADONLY_COLLECTION_T = "System.Collections.Generic.IReadOnlyCollection`1";
        internal const string IREADONLY_LIST_T = "System.Collections.Generic.IReadOnlyList`1";
        internal const string HASHSET_T = "System.Collections.Generic.HashSet`1";
        internal const string ISET_T = "System.Collections.Generic.ISet`1";
        internal const string TASK = "System.Threading.Tasks.Task";
        internal const string TASK_T = "System.Threading.Tasks.Task`1";
        internal const string VALUE_TASK_T = "System.Threading.Tasks.ValueTask`1";
        internal const string IDATA_LOADER_RESULT_T = "GraphQL.DataLoader.IDataLoaderResult`1";
    }
}
