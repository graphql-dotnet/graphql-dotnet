using System.Text;
using GraphQL.Analyzers.SourceGenerators.Providers;
using GraphQL.Analyzers.SourceGenerators.Transformers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

public partial class CandidateClassTransformerTests
{
    /// <summary>
    /// A minimal test generator that uses CandidateProvider and AttributeDataTransformer to report 
    /// extracted attribute data. This isolates testing of the transformation logic from code generation.
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

                foreach (var (candidate, symbols) in items.OrderBy(x => x.Left.ClassSymbol.Name))
                {
                    var schemaData = CandidateClassTransformer.Transform(candidate, symbols);

                    if (schemaData == null)
                        continue;

                    var data = schemaData.Value;

                    sb.AppendLine($"// Schema: {data.SchemaClass.Name}");
                    sb.AppendLine("//");

                    // Query Type
                    if (data.QueryType.HasValue)
                    {
                        var typeInfo = data.QueryType.Value;
                        var kind = typeInfo.IsClrType ? "CLR" : "GraphType";
                        sb.AppendLine($"// QueryType: {typeInfo.TypeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} ({kind})");
                    }
                    else
                    {
                        sb.AppendLine("// QueryType: (none)");
                    }
                    sb.AppendLine("//");

                    // Mutation Type
                    if (data.MutationType.HasValue)
                    {
                        var typeInfo = data.MutationType.Value;
                        var kind = typeInfo.IsClrType ? "CLR" : "GraphType";
                        sb.AppendLine($"// MutationType: {typeInfo.TypeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} ({kind})");
                    }
                    else
                    {
                        sb.AppendLine("// MutationType: (none)");
                    }
                    sb.AppendLine("//");

                    // Subscription Type
                    if (data.SubscriptionType.HasValue)
                    {
                        var typeInfo = data.SubscriptionType.Value;
                        var kind = typeInfo.IsClrType ? "CLR" : "GraphType";
                        sb.AppendLine($"// SubscriptionType: {typeInfo.TypeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} ({kind})");
                    }
                    else
                    {
                        sb.AppendLine("// SubscriptionType: (none)");
                    }
                    sb.AppendLine("//");

                    // Output Types
                    sb.AppendLine($"// OutputTypes: {data.OutputTypes.Length}");
                    for (int i = 0; i < data.OutputTypes.Length; i++)
                    {
                        var outputTypeInfo = data.OutputTypes[i];
                        var typeName = outputTypeInfo.TypeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                        var isInterfaceStr = outputTypeInfo.IsInterface.HasValue
                            ? $" (IsInterface: {(outputTypeInfo.IsInterface.Value ? "true" : "false")})"
                            : "";
                        sb.AppendLine($"//   [{i}] {typeName}{isInterfaceStr}");
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
                        var graphTypeInfo = data.GraphTypes[i];
                        var typeName = graphTypeInfo.TypeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                        var autoRegisterStr = !graphTypeInfo.AutoRegisterClrMapping
                            ? " (AutoRegisterClrMapping: false)"
                            : "";
                        sb.AppendLine($"//   [{i}] {typeName}{autoRegisterStr}");
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
