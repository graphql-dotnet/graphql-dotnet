using System.Text;
using GraphQL.Analyzers.SourceGenerators.Models;
using GraphQL.Analyzers.SourceGenerators.Providers;
using GraphQL.Analyzers.SourceGenerators.Transformers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

public partial class GeneratedTypeDataTransformerTests
{
    /// <summary>
    /// A test generator that uses GeneratedTypeDataTransformer to report the primitive-only data.
    /// </summary>
    [Generator]
    public class ReportingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get candidates and attribute symbols
            var candidateClasses = CandidateProvider.Create(context);
            var attributeSymbols = KnownSymbolsProvider.Create(context);

            // Combine them for transformation
            var candidatesWithSymbols = candidateClasses.Combine(attributeSymbols);

            // Transform and report
            context.RegisterSourceOutput(candidatesWithSymbols.Collect(), (spc, items) =>
            {
                if (items.Length == 0)
                    return;

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

                    // Transform to primitive-only data
                    var entries = GeneratedTypeDataTransformer.Transform(processedData, symbols).ToList();

                    sb.AppendLine($"// Schema: {attributeData.SchemaClass.Name}");
                    sb.AppendLine($"// Total Entries: {entries.Count}");
                    sb.AppendLine("//");

                    for (int i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];
                        sb.AppendLine($"// ========== Entry {i + 1} ==========");
                        sb.AppendLine($"// Namespace: {entry.Namespace ?? "null"}");
                        sb.AppendLine($"// PartialClassHierarchy: {string.Join(" > ", entry.PartialClassHierarchy.Select(p => p.Accessibility == ClassAccessibility.Public ? $"public {p.ClassName}" : p.Accessibility == ClassAccessibility.Private ? $"private {p.ClassName}" : $"internal {p.ClassName}"))}");

                        if (entry.SchemaClass is not null)
                        {
                            ReportSchemaClass(sb, entry.SchemaClass);
                        }
                        else if (entry.OutputGraphType is not null)
                        {
                            ReportOutputGraphType(sb, entry.OutputGraphType);
                        }
                        else if (entry.InputGraphType is not null)
                        {
                            ReportInputGraphType(sb, entry.InputGraphType);
                        }

                        sb.AppendLine("//");
                    }
                }

                spc.AddSource("GeneratedTypeDataReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            });
        }

        private static void ReportSchemaClass(StringBuilder sb, SchemaClassData data)
        {
            sb.AppendLine("// Type: SchemaClass");
            sb.AppendLine($"// HasConstructor: {data.HasConstructor}");
            sb.AppendLine($"// QueryRootTypeName: {data.QueryRootTypeName ?? "null"}");
            sb.AppendLine($"// MutationRootTypeName: {data.MutationRootTypeName ?? "null"}");
            sb.AppendLine($"// SubscriptionRootTypeName: {data.SubscriptionRootTypeName ?? "null"}");

            sb.AppendLine($"// RegisteredGraphTypes ({data.RegisteredGraphTypes.Count}):");
            foreach (var gt in data.RegisteredGraphTypes)
            {
                sb.AppendLine($"//   - {gt.FullyQualifiedGraphTypeName}");
                sb.AppendLine($"//     AotGenerated: {gt.AotGeneratedTypeName ?? "null"}");
                sb.AppendLine($"//     Override: {gt.OverrideTypeName ?? "null"}");
            }

            sb.AppendLine($"// TypeMappings ({data.TypeMappings.Count}):");
            foreach (var tm in data.TypeMappings)
            {
                sb.AppendLine($"//   {tm.FullyQualifiedClrTypeName} -> {tm.FullyQualifiedGraphTypeName}");
            }

            sb.AppendLine($"// ArrayListTypes ({data.ArrayListTypes.Count}): {string.Join(", ", data.ArrayListTypes)}");
            sb.AppendLine($"// GenericListTypes ({data.GenericListTypes.Count}): {string.Join(", ", data.GenericListTypes)}");
            sb.AppendLine($"// HashSetTypes ({data.HashSetTypes.Count}): {string.Join(", ", data.HashSetTypes)}");
        }

        private static void ReportOutputGraphType(StringBuilder sb, OutputGraphTypeData data)
        {
            sb.AppendLine("// Type: OutputGraphType");
            sb.AppendLine($"// IsInterface: {data.IsInterface}");
            sb.AppendLine($"// FullyQualifiedClrTypeName: {data.FullyQualifiedClrTypeName}");
            sb.AppendLine($"// GraphTypeClassName: {data.GraphTypeClassName}");
            sb.AppendLine($"// InstanceSource: {data.InstanceSource}");

            sb.AppendLine($"// SelectedMembers ({data.SelectedMembers.Count}):");
            foreach (var member in data.SelectedMembers)
            {
                sb.AppendLine($"//   - {member.MemberKind} {member.MemberName}");
                sb.AppendLine($"//     DeclaringType: {member.DeclaringTypeFullyQualifiedName ?? "same as CLR type"}");
                sb.AppendLine($"//     IsStatic: {member.IsStatic}");
                if (member.MethodParameters.Count > 0)
                {
                    sb.AppendLine($"//     Parameters ({member.MethodParameters.Count}):");
                    foreach (var param in member.MethodParameters)
                    {
                        sb.AppendLine($"//       - {param.FullyQualifiedTypeName}");
                    }
                }
            }

            if (data.ConstructorData is not null)
            {
                var ctor = data.ConstructorData;
                sb.AppendLine($"// ConstructorData:");
                sb.AppendLine($"//   Parameters ({ctor.Parameters.Count}):");
                foreach (var param in ctor.Parameters)
                {
                    sb.AppendLine($"//     - {param.FullyQualifiedTypeName ?? "IResolveFieldContext"}");
                }
                sb.AppendLine($"//   RequiredProperties ({ctor.RequiredProperties.Count}):");
                foreach (var prop in ctor.RequiredProperties)
                {
                    sb.AppendLine($"//     - {prop.Name}: {prop.FullyQualifiedTypeName}");
                }
            }
        }

        private static void ReportInputGraphType(StringBuilder sb, InputGraphTypeData data)
        {
            sb.AppendLine("// Type: InputGraphType");
            sb.AppendLine($"// FullyQualifiedClrTypeName: {data.FullyQualifiedClrTypeName}");
            sb.AppendLine($"// GraphTypeClassName: {data.GraphTypeClassName}");

            sb.AppendLine($"// Members ({data.Members.Count}):");
            foreach (var member in data.Members)
            {
                sb.AppendLine($"//   - {member.MemberName}: {member.FullyQualifiedTypeName}");
                sb.AppendLine($"//     DeclaringType: {member.DeclaringTypeFullyQualifiedName ?? "same as CLR type"}");
            }

            sb.AppendLine($"// ConstructorParameters ({data.ConstructorParameters.Count}):");
            foreach (var param in data.ConstructorParameters)
            {
                sb.AppendLine($"//   - {param.MemberName}");
            }
        }
    }
}
