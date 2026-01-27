using System.Text;
using GraphQL.Analyzers.SourceGenerators.Providers;
using GraphQL.Analyzers.SourceGenerators.Transformers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

public partial class SchemaAttributeDataTransformerTests
{
    /// <summary>
    /// A minimal test generator that uses CandidateProvider, CandidateClassTransformer, and SchemaAttributeDataTransformer
    /// to report the results of schema data transformation. This isolates testing of SchemaAttributeDataTransformer's
    /// type graph walking and discovery logic.
    /// </summary>
    [Generator]
    public class ReportingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get candidates and attribute symbols
            var candidateClasses = CandidateProvider.CreateCandidateProvider(context);
            var attributeSymbols = KnownSymbolsProvider.CreateAttributeSymbolsProvider(context);

            // Combine them for transformation
            var candidatesWithSymbols = candidateClasses.Combine(attributeSymbols);

            // Transform and report
            context.RegisterSourceOutput(candidatesWithSymbols.Collect(), (spc, items) =>
            {
                if (items.Length == 0)
                {
                    // No candidates matched
                    return;
                }

                var sb = new StringBuilder();
                bool first = true;

                foreach (var (candidate, symbols) in items.OrderBy(x => x.Left.ClassSymbol.Name))
                {
                    if (first)
                        first = false;
                    else
                        sb.AppendLine();

                    // First, extract attribute data from the candidate
                    var schemaData = CandidateClassTransformer.Transform(candidate, symbols);

                    if (schemaData == null)
                        continue;

                    var attributeData = schemaData.Value;

                    // Now, transform the attribute data using SchemaAttributeDataTransformer
                    var processedData = SchemaAttributeDataTransformer.Transform(attributeData, symbols);

                    sb.AppendLine($"// Schema: {attributeData.SchemaClass.Name}");
                    sb.AppendLine("//");

                    // Query Root GraphType
                    if (processedData.QueryRootGraphType != null)
                    {
                        sb.AppendLine($"// QueryRootGraphType: {processedData.QueryRootGraphType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    else
                    {
                        sb.AppendLine("// QueryRootGraphType: (none)");
                    }
                    sb.AppendLine("//");

                    // Mutation Root GraphType
                    if (processedData.MutationRootGraphType != null)
                    {
                        sb.AppendLine($"// MutationRootGraphType: {processedData.MutationRootGraphType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    else
                    {
                        sb.AppendLine("// MutationRootGraphType: (none)");
                    }
                    sb.AppendLine("//");

                    // Subscription Root GraphType
                    if (processedData.SubscriptionRootGraphType != null)
                    {
                        sb.AppendLine($"// SubscriptionRootGraphType: {processedData.SubscriptionRootGraphType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    else
                    {
                        sb.AppendLine("// SubscriptionRootGraphType: (none)");
                    }
                    sb.AppendLine("//");

                    // Discovered GraphTypes
                    sb.AppendLine($"// DiscoveredGraphTypes: {processedData.DiscoveredGraphTypes.Count}");
                    for (int i = 0; i < processedData.DiscoveredGraphTypes.Count; i++)
                    {
                        var graphType = (ITypeSymbol)processedData.DiscoveredGraphTypes[i];
                        sb.AppendLine($"//   [{i}] {graphType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // Output CLR Type Mappings
                    sb.AppendLine($"// OutputClrTypeMappings: {processedData.OutputClrTypeMappings.Count}");
                    for (int i = 0; i < processedData.OutputClrTypeMappings.Count; i++)
                    {
                        var mapping = processedData.OutputClrTypeMappings[i];
                        var clrType = (ITypeSymbol)mapping.ClrType;
                        var graphType = (ITypeSymbol)mapping.GraphType;
                        sb.AppendLine($"//   [{i}] {clrType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} -> {graphType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // Input CLR Type Mappings
                    sb.AppendLine($"// InputClrTypeMappings: {processedData.InputClrTypeMappings.Count}");
                    for (int i = 0; i < processedData.InputClrTypeMappings.Count; i++)
                    {
                        var mapping = processedData.InputClrTypeMappings[i];
                        var clrType = (ITypeSymbol)mapping.ClrType;
                        var graphType = (ITypeSymbol)mapping.GraphType;
                        sb.AppendLine($"//   [{i}] {clrType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} -> {graphType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // Input List Types
                    sb.AppendLine($"// InputListTypes: {processedData.InputListTypes.Count}");
                    for (int i = 0; i < processedData.InputListTypes.Count; i++)
                    {
                        var listType = (ITypeSymbol)processedData.InputListTypes[i];
                        sb.AppendLine($"//   [{i}] {listType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                }

                spc.AddSource("SchemaTransformationReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            });
        }
    }
}
