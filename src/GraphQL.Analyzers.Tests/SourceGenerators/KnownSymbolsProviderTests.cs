using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.KnownSymbolsProviderTests.ReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

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

            """);
    }
}
