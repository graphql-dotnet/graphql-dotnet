using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.KnownSymbolsProviderTests.ReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/*
 * 
 * These tests do not rely on other components
 * 
 */

/// <summary>
/// Tests for AttributeSymbolsProvider symbol resolution logic.
/// These tests verify that the provider correctly resolves all AOT attribute type symbols.
/// Uses TestAttributeSymbolsReportingGenerator to isolate testing of AttributeSymbolsProvider.
/// </summary>
public partial class KnownSymbolsProviderTests
{
    [Fact]
    public async Task ResolvesAllAttributeSymbols()
    {
        const string source =
            """
            public class Dummy { }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        output.ShouldBe(
            """
            // SUCCESS:

            // ========= AttributeSymbolsReport.g.cs ============

            // Attribute Symbols Resolution:
            //
            // AotQueryType: GraphQL.AotQueryTypeAttribute<T>
            // AotMutationType: GraphQL.AotMutationTypeAttribute<T>
            // AotSubscriptionType: GraphQL.AotSubscriptionTypeAttribute<T>
            // AotOutputType: GraphQL.AotOutputTypeAttribute<T>
            // AotInputType: GraphQL.AotInputTypeAttribute<T>
            // AotGraphType: GraphQL.AotGraphTypeAttribute<TGraphType>
            // AotTypeMapping: GraphQL.AotTypeMappingAttribute<TClrType, TGraphType>
            // AotListType: GraphQL.AotListTypeAttribute<TListType>
            // AotRemapType: GraphQL.AotRemapTypeAttribute<TGraphType, TGraphTypeImplementation>
            // IGraphType: GraphQL.Types.IGraphType
            // NonNullGraphType: GraphQL.Types.NonNullGraphType<T>
            // ListGraphType: GraphQL.Types.ListGraphType<T>
            // GraphQLClrInputTypeReference: GraphQL.Types.GraphQLClrInputTypeReference<T>
            // GraphQLClrOutputTypeReference: GraphQL.Types.GraphQLClrOutputTypeReference<T>
            // IgnoreAttribute: GraphQL.IgnoreAttribute
            // DoNotMapClrTypeAttribute: GraphQL.DoNotMapClrTypeAttribute
            // ClrTypeMappingAttribute: GraphQL.ClrTypeMappingAttribute
            // MemberScanAttribute: GraphQL.MemberScanAttribute
            // ParameterAttribute: GraphQL.ParameterAttribute
            // GraphQLConstructorAttribute: GraphQL.GraphQLConstructorAttribute
            // InstanceSourceAttribute: GraphQL.InstanceSourceAttribute
            // InputTypeAttributeT: GraphQL.InputTypeAttribute<TGraphType>
            // InputTypeAttribute: GraphQL.InputTypeAttribute
            // InputBaseTypeAttributeT: GraphQL.InputBaseTypeAttribute<TGraphType>
            // InputBaseTypeAttribute: GraphQL.InputBaseTypeAttribute
            // OutputTypeAttributeT: GraphQL.OutputTypeAttribute<TGraphType>
            // OutputTypeAttribute: GraphQL.OutputTypeAttribute
            // OutputBaseTypeAttributeT: GraphQL.OutputBaseTypeAttribute<TGraphType>
            // OutputBaseTypeAttribute: GraphQL.OutputBaseTypeAttribute
            // BaseGraphTypeAttributeT: GraphQL.BaseGraphTypeAttribute<TGraphType>
            // BaseGraphTypeAttribute: GraphQL.BaseGraphTypeAttribute
            // IEnumerableT: System.Collections.Generic.IEnumerable<T>
            // IListT: System.Collections.Generic.IList<T>
            // ListT: System.Collections.Generic.List<T>
            // ICollectionT: System.Collections.Generic.ICollection<T>
            // IReadOnlyCollectionT: System.Collections.Generic.IReadOnlyCollection<T>
            // IReadOnlyListT: System.Collections.Generic.IReadOnlyList<T>
            // HashSetT: System.Collections.Generic.HashSet<T>
            // ISetT: System.Collections.Generic.ISet<T>
            // Task: System.Threading.Tasks.Task
            // TaskT: System.Threading.Tasks.Task<TResult>
            // ValueTaskT: System.Threading.Tasks.ValueTask<TResult>
            // IDataLoaderResultT: GraphQL.DataLoader.IDataLoaderResult<T>
            // IObservableT: System.IObservable<T>
            // IAsyncEnumerableT: System.Collections.Generic.IAsyncEnumerable<T>
            // IResolveFieldContext: GraphQL.IResolveFieldContext
            // CancellationToken: System.Threading.CancellationToken
            // IInputObjectGraphType: GraphQL.Types.IInputObjectGraphType
            // IObjectGraphType: GraphQL.Types.IObjectGraphType
            // IInterfaceGraphType: GraphQL.Types.IInterfaceGraphType
            // ScalarGraphType: GraphQL.Types.ScalarGraphType
            // ComplexGraphType: GraphQL.Types.ComplexGraphType<TSourceType>
            // EnumerationGraphType: GraphQL.Types.EnumerationGraphType<TEnum>
            // AutoRegisteringObjectGraphType: GraphQL.Types.AutoRegisteringObjectGraphType<TSourceType>
            // AutoRegisteringInputObjectGraphType: GraphQL.Types.AutoRegisteringInputObjectGraphType<TSourceType>
            // AutoRegisteringInterfaceGraphType: GraphQL.Types.AutoRegisteringInterfaceGraphType<TSourceType>
            //
            // BuiltInScalarMappings (22 mappings):
            //   int -> GraphQL.Types.IntGraphType
            //   long -> GraphQL.Types.LongGraphType
            //   double -> GraphQL.Types.FloatGraphType
            //   float -> GraphQL.Types.FloatGraphType
            //   decimal -> GraphQL.Types.DecimalGraphType
            //   string -> GraphQL.Types.StringGraphType
            //   bool -> GraphQL.Types.BooleanGraphType
            //   System.DateTime -> GraphQL.Types.DateTimeGraphType
            //   short -> GraphQL.Types.ShortGraphType
            //   ushort -> GraphQL.Types.UShortGraphType
            //   ulong -> GraphQL.Types.ULongGraphType
            //   uint -> GraphQL.Types.UIntGraphType
            //   byte -> GraphQL.Types.ByteGraphType
            //   sbyte -> GraphQL.Types.SByteGraphType
            //   System.Numerics.BigInteger -> GraphQL.Types.BigIntGraphType
            //   System.Half -> GraphQL.Types.HalfGraphType
            //   System.DateOnly -> GraphQL.Types.DateOnlyGraphType
            //   System.TimeOnly -> GraphQL.Types.TimeOnlyGraphType
            //   System.DateTimeOffset -> GraphQL.Types.DateTimeOffsetGraphType
            //   System.TimeSpan -> GraphQL.Types.TimeSpanSecondsGraphType
            //   System.Guid -> GraphQL.Types.IdGraphType
            //   System.Uri -> GraphQL.Types.UriGraphType

            """);
    }
}
