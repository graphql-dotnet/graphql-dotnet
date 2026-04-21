using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.Interceptors;

/// <summary>
/// Provides a value indicating whether field interceptors should be enabled based on MSBuild properties.
/// </summary>
internal static class InterceptorEnabledProvider
{
    /// <summary>
    /// Creates an incremental value provider that determines if interceptors should be enabled.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    /// <returns>An incremental value provider that returns true if interceptors are enabled, false otherwise.</returns>
    public static IncrementalValueProvider<bool> Create(IncrementalGeneratorInitializationContext context)
    {
        return context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                var globalOptions = provider.GlobalOptions;

                // Check for opt-in property: GraphQLEnableFieldInterceptors
                // This property is set by the user or automatically by GraphQL.Analyzers.Package
                if (globalOptions.TryGetValue("build_property.GraphQLEnableFieldInterceptors", out var enabledValue))
                {
                    return bool.TryParse(enabledValue, out var result) && result;
                }

                // Default: disabled
                return false;
            });
    }
}
