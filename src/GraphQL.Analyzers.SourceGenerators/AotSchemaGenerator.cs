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
        // This sets up handlers in Rosyln to track the specific attributes (such as [AotQueryType<>])
        //   that indicate a class is a candidate for code generation; then it combines these together
        //   into a single collection of candidate classes to be processed.
        var candidateClasses = CandidateProvider.Create(context);

        // Combine with the compilation
        var compilation = context.CompilationProvider;
        var candidatesWithCompilation = candidateClasses.Combine(compilation);

        // Process each candidate class and produces serialized data required for generation
        var processedCandidates = candidatesWithCompilation.SelectMany((pair, _) =>
        {
            var (candidate, compilation) = pair;

            // Get known symbols
            var symbols = KnownSymbolsProvider.Transform(compilation);

            // Extract the attributes from the candidate class (e.g. [AotInputType<>], [AotOutputType<>], etc.)
            var schemaData = CandidateClassTransformer.Transform(candidate, symbols);
            if (!schemaData.HasValue)
                return Array.Empty<GeneratedTypeEntry>();

            // Walks the referenced types to discover all types that need to be generated
            var data = SchemaAttributeDataTransformer.Transform(schemaData.Value, symbols);

            // Transform to primitive-only data suitable for code generation
            // Each GeneratedTypeEntry represents a single file to be generated, consisting of:
            //   a) the Configure method for the schema class and/or constructor
            //   b) an "auto-registering" input graph type class
            //   c) an "auto-registering" output (object/interface) graph type class
            // Scalars, unions and enums will not produce a distinct file
            var generatedTypeData = ProcessedSchemaDataTransformer.Transform(data, symbols);
            if (generatedTypeData == null)
                return Array.Empty<GeneratedTypeEntry>();

            // Primitive data is stored in immutable records so Roslyn can track changes efficiently
            return generatedTypeData;
        });

        // Register source output for each schema or type to be generated
        context.RegisterSourceOutput(processedCandidates, GenerateSource);
    }

    private static void GenerateSource(SourceProductionContext context, GeneratedTypeEntry data)
    {
        // Determine the class name for the schema being generated; use this as the base filename
        // Note: this does not include namespace or nesting, nor is there any guarantee of uniqueness
        var schemaClassName = data.PartialClassHierarchy[^1].ClassName;

        // Generate schema configuration, input graph type, and output graph type as needed
        // Note: GraphTypeClassName is guaranteed to be unique for each input/output type within the schema

        if (data.SchemaClass != null)
        {
            var content = SchemaConfigurationGenerator.Generate(data.Namespace, data.PartialClassHierarchy, data.SchemaClass);
            var filename = $"{schemaClassName}.g.cs";
            context.AddSource(filename, SourceText.From(content, Encoding.UTF8));
        }

        if (data.InputGraphType != null)
        {
            var content = InputGraphTypeGenerator.Generate(data.Namespace, data.PartialClassHierarchy, data.InputGraphType);
            var filename = $"{schemaClassName}_{data.InputGraphType.GraphTypeClassName}.g.cs";
            context.AddSource(filename, SourceText.From(content, Encoding.UTF8));
        }

        if (data.OutputGraphType != null)
        {
            var content = OutputGraphTypeGenerator.Generate(data.Namespace, data.PartialClassHierarchy, data.OutputGraphType);
            var filename = $"{schemaClassName}_{data.OutputGraphType.GraphTypeClassName}.g.cs";
            context.AddSource(filename, SourceText.From(content, Encoding.UTF8));
        }
    }
}
