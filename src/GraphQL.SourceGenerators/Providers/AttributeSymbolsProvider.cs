using GraphQL.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Providers;

/// <summary>
/// Provides a way to resolve attribute symbols for AOT compilation.
/// </summary>
public static class AttributeSymbolsProvider
{
    /// <summary>
    /// Creates a provider that resolves INamedTypeSymbol for all AOT attribute types.
    /// </summary>
    public static IncrementalValueProvider<AotAttributeSymbols> CreateAttributeSymbolsProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider.Select(static (compilation, _) =>
        {
            return new AotAttributeSymbols
            {
                AotQueryType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_QUERY_TYPE),
                AotMutationType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_MUTATION_TYPE),
                AotSubscriptionType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_SUBSCRIPTION_TYPE),
                AotOutputType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_OUTPUT_TYPE),
                AotInputType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_INPUT_TYPE),
                AotGraphType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_GRAPH_TYPE),
                AotTypeMapping = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_TYPE_MAPPING),
                AotListType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_LIST_TYPE),
                AotRemapType = compilation.GetTypeByMetadataName(Constants.AttributeNames.AOT_REMAP_TYPE),
                IGraphType = compilation.GetTypeByMetadataName(Constants.TypeNames.IGRAPH_TYPE)
            };
        });
    }
}
