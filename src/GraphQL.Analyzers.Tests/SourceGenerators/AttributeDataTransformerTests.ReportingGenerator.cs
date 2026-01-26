using System.Text;
using GraphQL.SourceGenerators.Providers;
using GraphQL.SourceGenerators.Transformers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

public partial class AttributeDataTransformerTests
{
    /// <summary>
    /// A minimal test generator that uses CandidateProvider and AttributeDataTransformer to report 
    /// extracted attribute data. This isolates testing of the transformation logic from code generation.
    /// </summary>
    [Generator]
#pragma warning disable RS1036 // Specify analyzer banned API enforcement setting
    public class ReportingGenerator : IIncrementalGenerator
#pragma warning restore RS1036 // Specify analyzer banned API enforcement setting
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get candidates and attribute symbols
            var candidateClasses = CandidateProvider.CreateCandidateProvider(context);
            var attributeSymbols = AttributeSymbolsProvider.CreateAttributeSymbolsProvider(context);

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

                foreach (var (candidate, symbols) in items.OrderBy(x => x.Left.ClassSymbol.Name))
                {
                    var schemaData = AttributeDataTransformer.Transform(candidate, symbols);

                    if (schemaData == null)
                        continue;

                    var data = schemaData.Value;

                    sb.AppendLine($"// Schema: {data.SchemaClass.Name}");
                    sb.AppendLine("//");

                    // Query Types
                    sb.AppendLine($"// QueryTypes: {data.QueryTypes.Length}");
                    for (int i = 0; i < data.QueryTypes.Length; i++)
                    {
                        var typeInfo = data.QueryTypes[i];
                        var kind = typeInfo.IsClrType ? "CLR" : "GraphType";
                        sb.AppendLine($"//   [{i}] {typeInfo.TypeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} ({kind})");
                    }
                    sb.AppendLine("//");

                    // Mutation Types
                    sb.AppendLine($"// MutationTypes: {data.MutationTypes.Length}");
                    for (int i = 0; i < data.MutationTypes.Length; i++)
                    {
                        var typeInfo = data.MutationTypes[i];
                        var kind = typeInfo.IsClrType ? "CLR" : "GraphType";
                        sb.AppendLine($"//   [{i}] {typeInfo.TypeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} ({kind})");
                    }
                    sb.AppendLine("//");

                    // Subscription Types
                    sb.AppendLine($"// SubscriptionTypes: {data.SubscriptionTypes.Length}");
                    for (int i = 0; i < data.SubscriptionTypes.Length; i++)
                    {
                        var typeInfo = data.SubscriptionTypes[i];
                        var kind = typeInfo.IsClrType ? "CLR" : "GraphType";
                        sb.AppendLine($"//   [{i}] {typeInfo.TypeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} ({kind})");
                    }
                    sb.AppendLine("//");

                    // Output Types
                    sb.AppendLine($"// OutputTypes: {data.OutputTypes.Length}");
                    for (int i = 0; i < data.OutputTypes.Length; i++)
                    {
                        sb.AppendLine($"//   [{i}] {data.OutputTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // Input Types
                    sb.AppendLine($"// InputTypes: {data.InputTypes.Length}");
                    for (int i = 0; i < data.InputTypes.Length; i++)
                    {
                        sb.AppendLine($"//   [{i}] {data.InputTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // Graph Types
                    sb.AppendLine($"// GraphTypes: {data.GraphTypes.Length}");
                    for (int i = 0; i < data.GraphTypes.Length; i++)
                    {
                        sb.AppendLine($"//   [{i}] {data.GraphTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // Type Mappings
                    sb.AppendLine($"// TypeMappings: {data.TypeMappings.Length}");
                    for (int i = 0; i < data.TypeMappings.Length; i++)
                    {
                        var mapping = data.TypeMappings[i];
                        sb.AppendLine($"//   [{i}] {mapping.FromType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} -> {mapping.ToType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // List Types
                    sb.AppendLine($"// ListTypes: {data.ListTypes.Length}");
                    for (int i = 0; i < data.ListTypes.Length; i++)
                    {
                        sb.AppendLine($"//   [{i}] {data.ListTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // Remap Types
                    sb.AppendLine($"// RemapTypes: {data.RemapTypes.Length}");
                    for (int i = 0; i < data.RemapTypes.Length; i++)
                    {
                        var mapping = data.RemapTypes[i];
                        sb.AppendLine($"//   [{i}] {mapping.FromType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} -> {mapping.ToType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                }

                spc.AddSource("AttributeDataReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            });
        }
    }
}
