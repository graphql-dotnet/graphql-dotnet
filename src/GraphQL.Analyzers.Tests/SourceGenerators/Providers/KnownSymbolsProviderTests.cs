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

        output.ShouldMatchApproved(o => o.NoDiff());
    }
}
