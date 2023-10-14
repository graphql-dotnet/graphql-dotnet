namespace GraphQL.Federation.Instrumentation
{
    /// <summary>
    /// Methods to add Apollo federated tracing metrics to an <see cref="ExecutionResult"/> instance.
    /// </summary>
    public static class FederatedTracingExtensions
    {
        private const string EXTENSION_KEY = "ftv1";

        /// <summary>
        /// Adds Apollo federated tracing metrics to an <see cref="ExecutionResult"/> instance,
        /// stored within <see cref="ExecutionResult.Extensions"/>["tracing"].
        /// Requires that the GraphQL document was executed with metrics enabled;
        /// see <see cref="ExecutionOptions.EnableMetrics"/>. With <see cref="FederatedInstrumentFieldMiddleware"/>
        /// installed, also includes metrics from field resolvers.
        /// </summary>
        /// <param name="executionResult">An <see cref="ExecutionResult"/> instance.</param>
        /// <param name="start">The UTC date and time that the GraphQL document began execution.</param>
        public static void EnrichWithApolloFederatedTracing(this ExecutionResult executionResult, DateTime start)
        {
            var perf = executionResult.Perf;
            if (perf == null)
                return;
            var errors = executionResult.Errors;
            (executionResult.Extensions ??= new Dictionary<string, object?>())[EXTENSION_KEY] = new FederatedTraceBuilder(perf, errors, start).ToProtoBase64();
        }
    }
}
