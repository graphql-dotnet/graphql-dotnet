using System.Collections.Immutable;
using GraphQL.Analyzers.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Providers;

/// <summary>
/// Provides a way to resolve attribute symbols for AOT compilation.
/// </summary>
public static class KnownSymbolsProvider
{
    /// <summary>
    /// Creates a provider that resolves INamedTypeSymbol for all AOT attribute types.
    /// </summary>
    public static IncrementalValueProvider<KnownSymbols> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider.Select(static (compilation, _) => Transform(compilation));
    }

    /// <summary>
    /// Resolves and aggregates all well-known GraphQL and AOT-related symbols from the compilation,
    /// including attribute types, core GraphQL interfaces, collection/task abstractions, and
    /// built-in scalar type mappings, producing a single immutable snapshot for downstream
    /// incremental generator steps.
    /// </summary>
    public static KnownSymbols Transform(Compilation compilation)
    {
        // Build the built-in scalar mappings
        var builtInMappings = new List<(INamedTypeSymbol ClrType, INamedTypeSymbol GraphType)>();

        // Local helper method to add a scalar mapping using SpecialType
        void AddScalarMappingSpecial(SpecialType specialType, string graphTypeName)
        {
            var clrType = compilation.GetSpecialType(specialType);
            var graphType = compilation.GetTypeByMetadataName(graphTypeName);

            if (clrType != null && graphType != null)
            {
                builtInMappings.Add((clrType, graphType));
            }
        }

        // Local helper method to add a scalar mapping from metadata names
        void AddScalarMapping(string clrTypeName, string graphTypeName)
        {
            var clrType = compilation.GetTypeByMetadataName(clrTypeName);
            var graphType = compilation.GetTypeByMetadataName(graphTypeName);

            if (clrType != null && graphType != null)
            {
                builtInMappings.Add((clrType, graphType));
            }
        }

        // Primitive types using GetSpecialType
        AddScalarMappingSpecial(SpecialType.System_Int32, Constants.TypeNames.INT_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_Int64, Constants.TypeNames.LONG_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_Double, Constants.TypeNames.FLOAT_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_Single, Constants.TypeNames.FLOAT_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_Decimal, Constants.TypeNames.DECIMAL_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_String, Constants.TypeNames.STRING_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_Boolean, Constants.TypeNames.BOOLEAN_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_DateTime, Constants.TypeNames.DATETIME_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_Int16, Constants.TypeNames.SHORT_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_UInt16, Constants.TypeNames.USHORT_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_UInt64, Constants.TypeNames.ULONG_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_UInt32, Constants.TypeNames.UINT_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_Byte, Constants.TypeNames.BYTE_GRAPH_TYPE);
        AddScalarMappingSpecial(SpecialType.System_SByte, Constants.TypeNames.SBYTE_GRAPH_TYPE);

        // Non-primitive types using GetTypeByMetadataName
        AddScalarMapping(Constants.TypeNames.BIG_INTEGER, Constants.TypeNames.BIGINT_GRAPH_TYPE);
        AddScalarMapping(Constants.TypeNames.HALF, Constants.TypeNames.HALF_GRAPH_TYPE);
        AddScalarMapping(Constants.TypeNames.DATE_ONLY, Constants.TypeNames.DATEONLY_GRAPH_TYPE);
        AddScalarMapping(Constants.TypeNames.TIME_ONLY, Constants.TypeNames.TIMEONLY_GRAPH_TYPE);
        AddScalarMapping(Constants.TypeNames.DATETIMEOFFSET, Constants.TypeNames.DATETIMEOFFSET_GRAPH_TYPE);
        AddScalarMapping(Constants.TypeNames.TIMESPAN, Constants.TypeNames.TIMESPAN_SECONDS_GRAPH_TYPE);
        AddScalarMapping(Constants.TypeNames.GUID, Constants.TypeNames.ID_GRAPH_TYPE);
        AddScalarMapping(Constants.TypeNames.URI, Constants.TypeNames.URI_GRAPH_TYPE);

        // Collect built-in scalar types
        List<INamedTypeSymbol?> builtInScalars = [
            compilation.GetTypeByMetadataName(Constants.TypeNames.COMPLEX_SCALAR_GRAPH_TYPE),
            compilation.GetTypeByMetadataName(Constants.TypeNames.DATE_GRAPH_TYPE),
            compilation.GetTypeByMetadataName(Constants.TypeNames.GUID_GRAPH_TYPE),
            compilation.GetTypeByMetadataName(Constants.TypeNames.TIMESPAN_MILLISECONDS_GRAPH_TYPE),
        ];
        builtInScalars.AddRange(builtInMappings.Select(m => m.GraphType).Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default));

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
            DoNotMapClrTypeAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.DO_NOT_MAP_CLR_TYPE),
            ClrTypeMappingAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.CLR_TYPE_MAPPING),
            MemberScanAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.MEMBER_SCAN),
            ParameterAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.PARAMETER_ATTRIBUTE),
            ParameterAttributeT = compilation.GetTypeByMetadataName(Constants.AttributeNames.PARAMETER_ATTRIBUTE_T),
            GraphQLConstructorAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.GRAPHQL_CONSTRUCTOR),
            InstanceSourceAttribute = compilation.GetTypeByMetadataName(Constants.AttributeNames.INSTANCE_SOURCE),
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
            IObservableT = compilation.GetTypeByMetadataName(Constants.TypeNames.IOBSERVABLE_T),
            IAsyncEnumerableT = compilation.GetTypeByMetadataName(Constants.TypeNames.IASYNC_ENUMERABLE_T),
            IResolveFieldContext = compilation.GetTypeByMetadataName(Constants.TypeNames.IRESOLVE_FIELD_CONTEXT),
            CancellationToken = compilation.GetTypeByMetadataName(Constants.TypeNames.CANCELLATION_TOKEN),
            IInputObjectGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.IINPUT_OBJECT_GRAPH_TYPE),
            IObjectGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.IOBJECT_GRAPH_TYPE),
            IInterfaceGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.IINTERFACE_GRAPH_TYPE),
            ScalarGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.SCALAR_GRAPH_TYPE),
            ComplexGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.COMPLEX_GRAPH_TYPE),
            EnumerationGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.ENUMERATION_GRAPH_TYPE),
            AutoRegisteringObjectGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.AUTO_REGISTERING_OBJECT_GRAPH_TYPE),
            AutoRegisteringInputObjectGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.AUTO_REGISTERING_INPUT_OBJECT_GRAPH_TYPE),
            AutoRegisteringInterfaceGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.AUTO_REGISTERING_INTERFACE_GRAPH_TYPE),
            BuiltInScalarMappings = builtInMappings.ToImmutableArray(),
            BuiltInScalars = builtInScalars.Where(x => x != null).Select(x => x!).ToImmutableArray(),
        };
    }
}
