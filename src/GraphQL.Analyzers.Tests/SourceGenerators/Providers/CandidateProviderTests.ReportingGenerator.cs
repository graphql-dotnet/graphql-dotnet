using System.Text;
using GraphQL.Analyzers.SourceGenerators.Providers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

public partial class CandidateProviderTests
{
    /// <summary>
    /// A minimal test generator that uses CandidateProvider to report which candidates were matched.
    /// This isolates testing of the candidate filtering logic from the rest of the generator pipeline.
    /// </summary>
    [Generator]
    public class ReportingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Use the real CandidateProvider - this is what we're testing
            var candidateClasses = CandidateProvider.Create(context);

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

                foreach (var candidate in candidates.OrderBy(c => c.ClassSymbol.Name))
                {
                    var classSymbol = candidate.ClassSymbol;

                    var className = classSymbol.Name;

                    // Get namespace including containing types
                    var parts = new List<string>();

                    // Add namespace
                    if (classSymbol.ContainingNamespace != null && !classSymbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        parts.Add(classSymbol.ContainingNamespace.ToDisplayString());
                    }

                    // Add containing types
                    var containingType = classSymbol.ContainingType;
                    var containingTypes = new Stack<string>();
                    while (containingType != null)
                    {
                        containingTypes.Push(containingType.Name);
                        containingType = containingType.ContainingType;
                    }
                    parts.AddRange(containingTypes);

                    var namespaceName = string.Join(".", parts);

                    // Count total attributes from the symbol (which includes all partial declarations)
                    // This properly handles partial classes with attributes spread across multiple declarations
                    var totalAttributes = classSymbol.GetAttributes().Length;

                    sb.AppendLine();
                    sb.AppendLine($"// {className}");
                    sb.AppendLine($"//   Namespace: {namespaceName}");
                    sb.AppendLine($"//   AttributeCount: {totalAttributes}");
                }

                spc.AddSource("CandidatesReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            });
        }
    }
}
