namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Methods to add Apollo tracing metrics to an <see cref="ExecutionResult"/> instance.
    /// </summary>
    public static class ApolloTracingExtensions
    {
        /// <summary>
        /// Adds Apollo tracing metrics to an <see cref="ExecutionResult"/> instance,
        /// stored within <see cref="ExecutionResult.Extensions"/>["tracing"].
        /// Requires that the GraphQL document was executed with metrics enabled;
        /// see <see cref="ExecutionOptions.EnableMetrics"/>. With <see cref="InstrumentFieldsMiddleware"/>
        /// installed, also includes metrics from field resolvers.
        /// </summary>
        /// <param name="result">An <see cref="ExecutionResult"/> instance.</param>
        /// <param name="start">The date and time that the GraphQL document began execution. If not UTC, this value will be converted to UTC.</param>
        public static void EnrichWithApolloTracing(this ExecutionResult result, DateTime start)
        {
            var perf = result?.Perf;
            if (perf != null)
                (result!.Extensions ??= new())["tracing"] = CreateTrace(perf, start);
        }

        /// <summary>
        /// Initializes an <see cref="ApolloTrace"/> instance and populates it with performance
        /// metrics gathered during the GraphQL document execution.
        /// </summary>
        /// <param name="perf">A list of performance records; typically as returned from <see cref="Metrics.Finish"/>.</param>
        /// <param name="start">The date and time that the GraphQL document began execution. If not UTC, this value will be converted to UTC.</param>
        public static ApolloTrace CreateTrace(PerfRecord[] perf, DateTime start)
        {
            var operationStat = perf.Single(x => x.Category == "operation"); // always exists
            var trace = new ApolloTrace(start, operationStat.Duration);

            var documentStats = perf.Where(x => x.Category == "document");

            var parsingStat = documentStats.FirstOrDefault(x => x.Subject == "Building document");
            if (parsingStat != null) // can be null if exception occurred
            {
                trace.Parsing.StartOffset = ApolloTrace.ConvertTime(parsingStat.Start);
                trace.Parsing.Duration = ApolloTrace.ConvertTime(parsingStat.Duration);
            }

            var validationStat = documentStats.FirstOrDefault(x => x.Subject == "Validating document");
            if (validationStat != null) // can be null if exception occurred
            {
                trace.Validation.StartOffset = ApolloTrace.ConvertTime(validationStat.Start);
                trace.Validation.Duration = ApolloTrace.ConvertTime(validationStat.Duration);
            }

            var fieldStats = perf.Where(x => x.Category == "field");
            foreach (var fieldStat in fieldStats)
            {
                trace.Execution.Resolvers.Add(
                    new ApolloTrace.ResolverTrace
                    {
                        FieldName = fieldStat.MetaField<string>("fieldName"),
                        Path = fieldStat.MetaField<IEnumerable<object>>("path")!.ToList(),
                        ParentType = fieldStat.MetaField<string>("typeName"),
                        ReturnType = fieldStat.MetaField<string>("returnTypeName"),
                        StartOffset = ApolloTrace.ConvertTime(fieldStat.Start),
                        Duration = ApolloTrace.ConvertTime(fieldStat.Duration),
                    });
            }

            return trace;
        }
    }
}
