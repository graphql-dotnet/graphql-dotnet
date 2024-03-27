using GraphQL.DI;
using GraphQL.Federation.Instrumentation;
using GraphQL.Instrumentation;

namespace GraphQL;

/// <summary>
/// Federation extension methods for <see cref="IGraphQLBuilder"/>.
/// </summary>
public static class FederationGraphQLBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="InstrumentFieldsMiddleware"/> within the dependency injection framework and
    /// configures it to be installed within the schema, and configures responses to include Apollo
    /// Federated Tracing data when enabled via <see cref="ExecutionOptions.EnableMetrics"/>.
    /// When <paramref name="enableMetrics"/> is <see langword="true"/>, configures execution to set
    /// <see cref="ExecutionOptions.EnableMetrics"/> to <see langword="true"/>; otherwise leaves it unchanged.
    /// </summary>
    /// <remarks>
    /// Do not use in conjunction with <see cref="GraphQL.GraphQLBuilderExtensions.UseApolloTracing(IGraphQLBuilder, bool)">UseApolloTracing</see>
    /// or <see cref="InstrumentFieldsMiddleware"/> will be installed twice within the same schema.
    /// </remarks>
    public static IGraphQLBuilder UseApolloFederatedTracing(this IGraphQLBuilder builder, bool enableMetrics = true)
        => UseApolloFederatedTracing(builder, _ => enableMetrics);

    /// <summary>
    /// Registers <see cref="InstrumentFieldsMiddleware"/> within the dependency injection framework and
    /// configures it to be installed within the schema, and configures responses to include Apollo
    /// Federated Tracing data when enabled via <see cref="ExecutionOptions.EnableMetrics"/>.
    /// Configures execution to run <paramref name="enableMetricsPredicate"/> and when <see langword="true"/>, sets
    /// <see cref="ExecutionOptions.EnableMetrics"/> to <see langword="true"/>; otherwise leaves it unchanged.
    /// </summary>
    /// <remarks>
    /// Do not use in conjunction with <see cref="GraphQL.GraphQLBuilderExtensions.UseApolloTracing(IGraphQLBuilder, Func{ExecutionOptions, bool})">UseApolloTracing</see>
    /// or <see cref="InstrumentFieldsMiddleware"/> will be installed twice within the same schema.
    /// </remarks>
    public static IGraphQLBuilder UseApolloFederatedTracing(this IGraphQLBuilder builder, Func<ExecutionOptions, bool> enableMetricsPredicate)
    {
        if (enableMetricsPredicate == null)
            throw new ArgumentNullException(nameof(enableMetricsPredicate));

        return builder
            .UseMiddleware<InstrumentFieldsMiddleware>()
            .ConfigureExecution(async (options, next) =>
            {
                if (enableMetricsPredicate(options))
                    options.EnableMetrics = true;
                var start = DateTime.UtcNow;
                var ret = await next(options).ConfigureAwait(false);
                if (options.EnableMetrics)
                {
                    ret.EnrichWithApolloFederatedTracing(start);
                }
                return ret;
            });
    }
}
