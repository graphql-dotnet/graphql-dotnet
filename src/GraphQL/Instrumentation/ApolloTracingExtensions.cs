using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Instrumentation
{
    public static class ApolloTracingExtensions
    {
        public static void EnrichWithApolloTracing(this ExecutionResult result, DateTime start)
        {
            var perf = result?.Perf;
            if (perf == null)
            {
                return;
            }

            var trace = CreateTrace(result.Operation, perf, start);
            if (result.Extensions == null)
            {
                result.Extensions = new Dictionary<string, object>();
            }
            result.Extensions["tracing"] = trace;
        }

        public static ApolloTrace CreateTrace(
            Operation operation,
            PerfRecord[] perf,
            DateTime start)
        {
            var operationStat = perf.Single(x => x.Category == "operation"); // always exists
            var trace = new ApolloTrace(start, operationStat.Duration);

            var documentStats = perf.Where(x => x.Category == "document");

            var parsingStat = documentStats.FirstOrDefault(x => x.Subject == "Building document");
            if (parsingStat != null) // can be null if exception occured
            {
                trace.Parsing.StartOffset = ApolloTrace.ConvertTime(parsingStat.Start);
                trace.Parsing.Duration = ApolloTrace.ConvertTime(parsingStat.Duration);
            }

            var validationStat = documentStats.FirstOrDefault(x => x.Subject == "Validating document");
            if (validationStat != null) // can be null if exception occured
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
                        Path = fieldStat.MetaField<IEnumerable<object>>("path").ToList(),
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
