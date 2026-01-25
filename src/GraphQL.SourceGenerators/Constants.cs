namespace GraphQL.SourceGenerators;

internal static class Constants
{
    internal static class AttributeNames
    {
        internal const string AotQueryType = "GraphQL.AotQueryTypeAttribute";
        internal const string AotMutationType = "GraphQL.AotMutationTypeAttribute";
        internal const string AotSubscriptionType = "GraphQL.AotSubscriptionTypeAttribute";
        internal const string AotOutputType = "GraphQL.AotOutputTypeAttribute";
        internal const string AotInputType = "GraphQL.AotInputTypeAttribute";
        internal const string AotGraphType = "GraphQL.AotGraphTypeAttribute";
        internal const string AotTypeMapping = "GraphQL.AotTypeMappingAttribute";
        internal const string AotListType = "GraphQL.AotListTypeAttribute";
        internal const string AotRemapType = "GraphQL.AotRemapTypeAttribute";

        internal static readonly string[] All =
        {
            AotQueryType,
            AotMutationType,
            AotSubscriptionType,
            AotOutputType,
            AotInputType,
            AotGraphType,
            AotTypeMapping,
            AotListType,
            AotRemapType
        };
    }
}
