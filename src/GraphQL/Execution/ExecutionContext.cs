using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public class ExecutionContext : IExecutionContext
    {
        public Document Document { get; set; }

        public ISchema Schema { get; set; }

        public object RootValue { get; set; }

        public IDictionary<string, object> UserContext { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; } = new Fragments();

        public Variables Variables { get; set; }

        public ExecutionErrors Errors { get; set; } = new ExecutionErrors();

        public CancellationToken CancellationToken { get; set; }

        public Metrics Metrics { get; set; }

        public List<IDocumentExecutionListener> Listeners { get; set; }

        public bool ThrowOnUnhandledException { get; set; }

        public Action<UnhandledExceptionContext> UnhandledExceptionDelegate { get; set; }

        public int? MaxParallelExecutionCount { get; set; }

        public Dictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();
    }
}
