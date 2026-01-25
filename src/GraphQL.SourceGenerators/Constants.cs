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

        internal static readonly string[] All =
        {
            AOT_QUERY_TYPE,
            AOT_MUTATION_TYPE,
            AOT_SUBSCRIPTION_TYPE,
            AOT_OUTPUT_TYPE,
            AOT_INPUT_TYPE,
            AOT_GRAPH_TYPE,
            AOT_TYPE_MAPPING,
            AOT_LIST_TYPE,
            AOT_REMAP_TYPE
        };
    }
}
