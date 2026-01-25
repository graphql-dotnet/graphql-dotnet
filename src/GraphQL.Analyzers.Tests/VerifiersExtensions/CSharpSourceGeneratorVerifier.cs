using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.Tests.VerifiersExtensions;

public static partial class CSharpIncrementalGeneratorVerifier<TIncrementalGenerator>
    where TIncrementalGenerator : IIncrementalGenerator, new()
{
    /// <summary>
    /// Verifies that an incremental source generator produces the expected generated sources.
    /// </summary>
    public static async Task VerifyIncrementalGeneratorAsync(string source, params (string FileName, string Content)[] generatedSources)
    {
        var test = new Verifiers.CSharpIncrementalGeneratorVerifier<TIncrementalGenerator>.Test
        {
            TestCode = source,
        };

        foreach (var (fileName, content) in generatedSources)
        {
            test.TestState.GeneratedSources.Add((typeof(TIncrementalGenerator), fileName, content));
        }

        await test.RunAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that an incremental source generator produces no outputs for the given source.
    /// </summary>
    public static async Task VerifyIncrementalGeneratorAsync(string source)
    {
        var test = new Verifiers.CSharpIncrementalGeneratorVerifier<TIncrementalGenerator>.Test
        {
            TestCode = source,
        };

        await test.RunAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
