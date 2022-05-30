using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL.Instrumentation;

/// <summary>
/// Executes a GraphQL document and appends Apollo Tracing information via
/// <see cref="ApolloTracingExtensions.EnrichWithApolloTracing(GraphQL.ExecutionResult, DateTime)"/>
/// when complete, if <see cref="ExecutionOptions.EnableMetrics"/> is enabled.
/// </summary>
[Obsolete("Use AddApolloTracing instead of AddMetrics, which also appends Apollo Tracing data to the execution result. This class will be removed in v6.")]
public class ApolloTracingDocumentExecuter : DocumentExecuter
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    [Obsolete("Use constructor that accepts IConfigureExecution also; will be removed in v6")]
    public ApolloTracingDocumentExecuter(
        IDocumentBuilder documentBuilder,
        IDocumentValidator documentValidator,
        IComplexityAnalyzer complexityAnalyzer,
        IDocumentCache documentCache,
        IEnumerable<IConfigureExecutionOptions> configureExecutionOptions,
        IExecutionStrategySelector executionStrategySelector)
        : base(documentBuilder, documentValidator, complexityAnalyzer, documentCache, configureExecutionOptions, executionStrategySelector)
    {
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ApolloTracingDocumentExecuter(
        IDocumentBuilder documentBuilder,
        IDocumentValidator documentValidator,
        IComplexityAnalyzer complexityAnalyzer,
        IDocumentCache documentCache,
        IExecutionStrategySelector executionStrategySelector,
        IEnumerable<IConfigureExecution> configureExecutions,
        IEnumerable<IConfigureExecutionOptions> configureExecutionOptions)
        : base(documentBuilder, documentValidator, complexityAnalyzer, documentCache, executionStrategySelector, configureExecutions, configureExecutionOptions)
    {
    }

    /// <inheritdoc/>
    public override async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options)
    {
        var start = DateTime.UtcNow;
        var result = await base.ExecuteAsync(options).ConfigureAwait(false);
        if (options.EnableMetrics)
            result.EnrichWithApolloTracing(start);
        return result;
    }
}
