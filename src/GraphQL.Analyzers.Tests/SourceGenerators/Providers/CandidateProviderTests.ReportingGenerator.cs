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

                foreach (var candidate in candidates.OrderBy(c => c.ClassDeclarationSyntax.Identifier.Text))
                {
                    var classDecl = candidate.ClassDeclarationSyntax;
                    var semanticModel = candidate.SemanticModel;

                    var className = classDecl.Identifier.Text;

                    // Get namespace including containing types
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                    var namespaceName = string.Empty;
                    if (classSymbol != null)
                    {
                        var parts = new System.Collections.Generic.List<string>();

                        // Add namespace
                        if (classSymbol.ContainingNamespace != null && !classSymbol.ContainingNamespace.IsGlobalNamespace)
                        {
                            parts.Add(classSymbol.ContainingNamespace.ToDisplayString());
                        }

                        // Add containing types
                        var containingType = classSymbol.ContainingType;
                        var containingTypes = new System.Collections.Generic.Stack<string>();
                        while (containingType != null)
                        {
                            containingTypes.Push(containingType.Name);
                            containingType = containingType.ContainingType;
                        }
                        parts.AddRange(containingTypes);

                        namespaceName = string.Join(".", parts);
                    }

                    // Check if partial
                    var isPartial = classDecl.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword);

                    // Count total attributes from the symbol (which includes all partial declarations)
                    // This properly handles partial classes with attributes spread across multiple declarations
                    var totalAttributes = classSymbol?.GetAttributes().Length ?? 0;

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
}
