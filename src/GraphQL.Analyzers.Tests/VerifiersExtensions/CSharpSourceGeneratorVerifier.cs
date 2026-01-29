using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

    /// <summary>
    /// Runs the incremental source generator and returns a formatted string output suitable for snapshot testing.
    /// Returns SUCCESS with generated files or FAILURE with diagnostics.
    /// </summary>
    public static async Task<string> GetGeneratorOutputAsync(string source)
    {
        // Parse the source code
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12));

        // Get references (similar to how Test class sets them up)
        var references = await ReferenceResolver.ReferenceAssemblies
            .ResolveAsync(LanguageNames.CSharp, CancellationToken.None)
            .ConfigureAwait(false);

        // Create compilation with references
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references.Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(GraphQL.Types.Schema).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GraphQLParser.ROM).Assembly.Location),
            }),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create the generator and driver
        var generator = new TIncrementalGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Run the generator
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        // Get the results
        var runResult = driver.GetRunResult();

        // Check for diagnostics (errors/warnings from the generator)
        var generatorDiagnostics = runResult.Diagnostics
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToList();

        var compilationDiagnostics = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        // Check if there are errors
        if (generatorDiagnostics.Count > 0 || compilationDiagnostics.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// FAILURE:");
            sb.AppendLine();

            foreach (var diagnostic in generatorDiagnostics.Concat(compilationDiagnostics)
                .OrderBy(d => d.Location.SourceSpan.Start))
            {
                sb.AppendLine(diagnostic.ToString());
            }

            // Still include any generated files
            var generatedSources = runResult.Results.SelectMany(r => r.GeneratedSources).ToList();
            if (generatedSources.Count > 0)
            {
                sb.AppendLine();
                var orderedSources = generatedSources.OrderBy(s => s.HintName).ToList();
                for (int i = 0; i < orderedSources.Count; i++)
                {
                    var generatedSource = orderedSources[i];
                    sb.AppendLine($"// ========= {generatedSource.HintName} ============");
                    sb.AppendLine();
                    sb.Append(generatedSource.SourceText.ToString());
                    if (i < orderedSources.Count - 1)
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        // Success case
        var successSources = runResult.Results.SelectMany(r => r.GeneratedSources).ToList();
        if (successSources.Count == 0)
        {
            return "// SUCCESS:\n\n// No files generated";
        }

        var successBuilder = new StringBuilder();
        successBuilder.AppendLine("// SUCCESS:");
        successBuilder.AppendLine();

        var orderedSuccessSources = successSources.OrderBy(s => s.HintName).ToList();
        for (int i = 0; i < orderedSuccessSources.Count; i++)
        {
            var generatedSource = orderedSuccessSources[i];
            successBuilder.AppendLine($"// ========= {generatedSource.HintName} ============");
            successBuilder.AppendLine();
            successBuilder.Append(generatedSource.SourceText.ToString());
            if (i < orderedSuccessSources.Count - 1)
            {
                successBuilder.AppendLine();
                successBuilder.AppendLine();
            }
        }

        return successBuilder.ToString();
    }
}
