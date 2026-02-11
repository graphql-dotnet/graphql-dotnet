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
        //   into a stream of candidate classes to be processed.
        var candidateClasses = CandidateProvider.Create(context);

        // Collect all candidate classes together and combine with the compilation
        var compilation = context.CompilationProvider;
        var candidatesWithCompilation = candidateClasses.Collect().Combine(compilation);

        // Process the candidate classes and produce serialized data required for generation
        var processedCandidates = candidatesWithCompilation.SelectMany((candidatesWithCompilation, cancellationToken) =>
        {
            var (candidates, compilation) = candidatesWithCompilation;

            // If there are no candidates, return an empty array immediately to avoid unnecessary processing
            if (candidates.Length == 0)
                return Array.Empty<GeneratedTypeEntry>();

            // Get known symbols for the compilation; this allows us to avoid repeatedly looking up the same symbols for each candidate class
            var symbols = KnownSymbolsProvider.Transform(compilation);

            // Process each candidate class
            List<GeneratedTypeEntry>? classes = candidates.Length == 1 ? null : new List<GeneratedTypeEntry>();
            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Extract the attributes from the candidate class (e.g. [AotInputType<>], [AotOutputType<>], etc.)
                var schemaData = CandidateClassTransformer.Transform(candidate, symbols);
                if (!schemaData.HasValue)
                    continue;

                cancellationToken.ThrowIfCancellationRequested();

                // Walks the referenced types to discover all types that need to be generated
                var data = SchemaAttributeDataTransformer.Transform(schemaData.Value, symbols);

                cancellationToken.ThrowIfCancellationRequested();

                // Transform to primitive-only data suitable for code generation
                // Each GeneratedTypeEntry represents a single file to be generated, consisting of either:
                //   a) the Configure method for the schema class and/or constructor, or
                //   b) an "auto-registering" input graph type class, or
                //   c) an "auto-registering" output (object/interface) graph type class
                // Scalars, unions and enums will not produce a distinct file
                var generatedTypeData = ProcessedSchemaDataTransformer.Transform(data, symbols);
                if (generatedTypeData != null)
                {
                    // Primitive data is stored in immutable records so Roslyn can track changes efficiently
                    if (candidates.Length == 1)
                        return generatedTypeData;
                    else
                        classes!.AddRange(generatedTypeData);
                }
            }

            return classes ?? Enumerable.Empty<GeneratedTypeEntry>();
        });

        // At this point we have a stream of GeneratedTypeEntry records, each containing all the information
        //   needed to generate a single file for either a schema configuration, input graph type, or output
        //   graph type. Roslyn will automatically handle incremental generation and only re-run the generator
        //   for entries (files) that have changed.

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
