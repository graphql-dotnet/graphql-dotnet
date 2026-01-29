using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace GraphQL.Analyzers.Tests.Verifiers;

public static partial class CSharpIncrementalGeneratorVerifier<TIncrementalGenerator>
    where TIncrementalGenerator : IIncrementalGenerator, new()
{
    public class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, DefaultVerifier>
    {
        public Test()
        {
            // Use default reference assemblies
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

            // Add required references for GraphQL types
            TestState.AdditionalReferences.Add(typeof(GraphQL.Types.Schema).Assembly);

            // Apply nullable warnings
            SolutionTransforms.Add((solution, projectId) =>
            {
                var compilationOptions = solution.GetProject(projectId)!.CompilationOptions;
                compilationOptions = compilationOptions!.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                return solution;
            });
        }

        protected override ParseOptions CreateParseOptions()
        {
            return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion.CSharp12);
        }

        protected override IEnumerable<Type> GetSourceGenerators()
        {
            yield return typeof(TIncrementalGenerator);
        }
    }
}
