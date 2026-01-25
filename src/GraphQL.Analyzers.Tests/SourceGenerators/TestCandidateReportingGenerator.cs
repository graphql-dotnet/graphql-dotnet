using System.Text;
using GraphQL.SourceGenerators.Providers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// A minimal test generator that uses CandidateProvider to report which candidates were matched.
/// This isolates testing of the candidate filtering logic from the rest of the generator pipeline.
/// </summary>
[Generator]
#pragma warning disable RS1036 // Specify analyzer banned API enforcement setting
public class TestCandidateReportingGenerator : IIncrementalGenerator
#pragma warning restore RS1036 // Specify analyzer banned API enforcement setting
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Use the real CandidateProvider - this is what we're testing
        var candidateClasses = CandidateProvider.CreateCandidateProvider(context);

        // Collect all candidates and generate a report
        context.RegisterSourceOutput(candidateClasses.Collect(), (spc, candidates) =>
        {
            if (candidates.Length == 0)
            {
                // Do not generate any source if no candidates matched
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("// Matched Candidates:");

            foreach (var candidate in candidates.OrderBy(c => c.ClassDeclarationSyntax.Identifier.Text))
            {
                var classDecl = candidate.ClassDeclarationSyntax;
                var semanticModel = candidate.SemanticModel;

                var className = classDecl.Identifier.Text;

                // Get namespace
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                var namespaceName = string.Empty;
                if (classSymbol?.ContainingNamespace != null && !classSymbol.ContainingNamespace.IsGlobalNamespace)
                {
                    namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                }

                // Check if partial
                var isPartial = classDecl.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword);

                // Count total attributes
                var totalAttributes = classDecl.AttributeLists.Sum(al => al.Attributes.Count);

                sb.AppendLine();
                sb.AppendLine($"// {className}");
                sb.AppendLine($"//   Namespace: {namespaceName}");
                sb.AppendLine($"//   IsPartial: {isPartial}");
                sb.AppendLine($"//   AttributeCount: {totalAttributes}");
            }

            spc.AddSource("CandidatesReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }
}
