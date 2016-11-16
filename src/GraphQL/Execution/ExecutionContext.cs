using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public class ExecutionContext
    {
        public ExecutionContext()
        {
            Fragments = new Fragments();
            Errors = new ExecutionErrors();
        }

        public Document Document { get; set; }

        public ISchema Schema { get; set; }

        public object RootValue { get; set; }

        public object UserContext { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public ExecutionErrors Errors { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public Metrics Metrics { get; set; }
    }
}
