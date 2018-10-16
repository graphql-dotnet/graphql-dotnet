using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Instrumentation
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ApolloTrace
    {
        public ApolloTrace(DateTime start, double durationMs)
        {
            this.StartTime = start;
            this.EndTime = start.AddMilliseconds(durationMs);
            this.Duration = ConvertTime(durationMs);
        }

        public int Version => 1;

        public DateTime StartTime { get; }

        public DateTime EndTime { get; }

        public long Duration { get; }

        public OperationTrace Parsing { get; } = new OperationTrace();

        public OperationTrace Validation { get; } = new OperationTrace();

        public ExecutionTrace Execution { get; } = new ExecutionTrace();

        public static long ConvertTime(double ms) => (long)(ms * 1000 * 1000);

        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public class OperationTrace
        {
            public long StartOffset { get; set; }

            public long Duration { get; set; }
        }

        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public class ExecutionTrace
        {
            public List<ResolverTrace> Resolvers { get; } = new List<ResolverTrace>();
        }

        public class ResolverTrace : OperationTrace
        {
            public List<object> Path { get; set; } = new List<object>();

            public string ParentType { get; set; }

            public string FieldName { get; set; }

            public string ReturnType { get; set; }
        }
    }
}
