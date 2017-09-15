using System;
using System.Collections.Generic;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;

namespace GraphQL.Subscription
{
    public class SubscriptionExecutionResult
    {
        public string Query { get; set; }

        public bool ExposeExceptions { get; set; }

        public Document Document { get; set; }

        public Operation Operation { get; set; }

        public ExecutionErrors Errors { get; set; }

        public IDictionary<string, IObservable<ExecutionResult>> Streams { get; set; }

        public PerfRecord[] Perf { get; set; }
    }
}
