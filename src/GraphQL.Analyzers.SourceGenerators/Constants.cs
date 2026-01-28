using System.Collections.Immutable;

namespace GraphQL.Analyzers.SourceGenerators;

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
        internal const string DO_NOT_MAP_CLR_TYPE = "GraphQL.DoNotMapClrTypeAttribute";
        internal const string CLR_TYPE_MAPPING = "GraphQL.ClrTypeMappingAttribute";
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
        internal const string GRAPHQL_CONSTRUCTOR = "GraphQL.GraphQLConstructorAttribute";
        internal const string INSTANCE_SOURCE = "GraphQL.InstanceSourceAttribute";

        /// <summary>
        /// Includes AotQueryType, AotMutationType, AotSubscriptionType, AotOutputType, AotInputType,
        /// AotGraphType, AotTypeMapping, AotListType, and AotRemapType.
        /// </summary>
        internal static readonly ImmutableArray<string> AllAot = ImmutableArray.Create(
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

    internal static class PropertyNames
    {
        internal const string KIND = "Kind";
        internal const string AUTO_REGISTER_CLR_MAPPING = "AutoRegisterClrMapping";
    }

    internal static class TypeNames
    {
        internal const string AOT_SCHEMA = "GraphQL.Types.AotSchema";
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
        internal const string COMPLEX_GRAPH_TYPE = "GraphQL.Types.ComplexGraphType`1";
        internal const string ENUMERATION_GRAPH_TYPE = "GraphQL.Types.EnumerationGraphType`1";
        internal const string AUTO_REGISTERING_OBJECT_GRAPH_TYPE = "GraphQL.Types.AutoRegisteringObjectGraphType`1";
        internal const string AUTO_REGISTERING_INPUT_OBJECT_GRAPH_TYPE = "GraphQL.Types.AutoRegisteringInputObjectGraphType`1";
        internal const string AUTO_REGISTERING_INTERFACE_GRAPH_TYPE = "GraphQL.Types.AutoRegisteringInterfaceGraphType`1";
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

        // Built-in scalar GraphTypes
        internal const string INT_GRAPH_TYPE = "GraphQL.Types.IntGraphType";
        internal const string LONG_GRAPH_TYPE = "GraphQL.Types.LongGraphType";
        internal const string BIGINT_GRAPH_TYPE = "GraphQL.Types.BigIntGraphType";
        internal const string FLOAT_GRAPH_TYPE = "GraphQL.Types.FloatGraphType";
        internal const string DECIMAL_GRAPH_TYPE = "GraphQL.Types.DecimalGraphType";
        internal const string STRING_GRAPH_TYPE = "GraphQL.Types.StringGraphType";
        internal const string BOOLEAN_GRAPH_TYPE = "GraphQL.Types.BooleanGraphType";
        internal const string DATETIME_GRAPH_TYPE = "GraphQL.Types.DateTimeGraphType";
        internal const string DATETIMEOFFSET_GRAPH_TYPE = "GraphQL.Types.DateTimeOffsetGraphType";
        internal const string TIMESPAN_SECONDS_GRAPH_TYPE = "GraphQL.Types.TimeSpanSecondsGraphType";
        internal const string ID_GRAPH_TYPE = "GraphQL.Types.IdGraphType";
        internal const string SHORT_GRAPH_TYPE = "GraphQL.Types.ShortGraphType";
        internal const string USHORT_GRAPH_TYPE = "GraphQL.Types.UShortGraphType";
        internal const string ULONG_GRAPH_TYPE = "GraphQL.Types.ULongGraphType";
        internal const string UINT_GRAPH_TYPE = "GraphQL.Types.UIntGraphType";
        internal const string BYTE_GRAPH_TYPE = "GraphQL.Types.ByteGraphType";
        internal const string SBYTE_GRAPH_TYPE = "GraphQL.Types.SByteGraphType";
        internal const string URI_GRAPH_TYPE = "GraphQL.Types.UriGraphType";
        internal const string DATEONLY_GRAPH_TYPE = "GraphQL.Types.DateOnlyGraphType";
        internal const string TIMEONLY_GRAPH_TYPE = "GraphQL.Types.TimeOnlyGraphType";
        internal const string HALF_GRAPH_TYPE = "GraphQL.Types.HalfGraphType";

        // Built-in CLR types (not available via GetSpecialType)
        internal const string BIG_INTEGER = "System.Numerics.BigInteger";
        internal const string HALF = "System.Half";
        internal const string DATE_ONLY = "System.DateOnly";
        internal const string TIME_ONLY = "System.TimeOnly";
        internal const string DATETIMEOFFSET = "System.DateTimeOffset";
        internal const string TIMESPAN = "System.TimeSpan";
        internal const string GUID = "System.Guid";
        internal const string URI = "System.Uri";
    }
}
