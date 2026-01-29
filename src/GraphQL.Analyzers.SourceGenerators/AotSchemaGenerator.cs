using System.Text;
using GraphQL.Analyzers.SourceGenerators.Generators;
using GraphQL.Analyzers.SourceGenerators.Models;
using GraphQL.Analyzers.SourceGenerators.Providers;
using GraphQL.Analyzers.SourceGenerators.Transformers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.SourceGenerators;

/// <summary>
/// Incremental source generator for AOT-compiled GraphQL schemas.
/// Generates partial class implementations for classes decorated with AOT attributes.
/// </summary>
[Generator]
public class AotSchemaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get all candidate classes
        var candidateClasses = CandidateProvider.Create(context);

        // Combine with known symbols if needed
        var knownSymbols = KnownSymbolsProvider.Create(context);

        var candidatesWithSymbols = candidateClasses.Combine(knownSymbols);

        var processedCandidates = candidatesWithSymbols.SelectMany((pair, _) =>
        {
            var candidate = pair.Left;
            var symbols = pair.Right;
            var schemaData = CandidateClassTransformer.Transform(candidate, symbols);
            if (!schemaData.HasValue)
                return Array.Empty<GeneratedTypeEntry>();
            var data = SchemaAttributeDataTransformer.Transform(schemaData.Value, symbols);
            var generatedTypeData = GeneratedTypeDataTransformer.Transform(data, symbols);
            if (generatedTypeData == null)
                return Array.Empty<GeneratedTypeEntry>();
            return generatedTypeData;
        });

        // Register source output for each schema or type to be generated
        context.RegisterSourceOutput(processedCandidates, GenerateSource);
    }

    private static void GenerateSource(SourceProductionContext context, GeneratedTypeEntry data)
    {
        if (data.SchemaClass != null)
        {
            var content = SchemaConfigurationGenerator.Generate(data.Namespace, data.PartialClassHierarchy, data.SchemaClass);
            var filename = $"{data.PartialClassHierarchy.Last().ClassName}.g.cs";
            context.AddSource(filename, SourceText.From(content, Encoding.UTF8));
        }
        if (data.InputGraphType != null)
        {
            var content = InputGraphTypeGenerator.Generate(data.Namespace, data.PartialClassHierarchy, data.InputGraphType);
            var filename = $"{data.PartialClassHierarchy.Last().ClassName}_{data.InputGraphType.GraphTypeClassName}.g.cs";
            context.AddSource(filename, SourceText.From(content, Encoding.UTF8));
        }
        if (data.OutputGraphType != null)
        {
            var content = OutputGraphTypeGenerator.Generate(data.Namespace, data.PartialClassHierarchy, data.OutputGraphType);
            var filename = $"{data.PartialClassHierarchy.Last().ClassName}_{data.OutputGraphType.GraphTypeClassName}.g.cs";
            context.AddSource(filename, SourceText.From(content, Encoding.UTF8));
        }
    }
}
