using GraphQL.Language;
using GraphQL.Types;
using System.Threading;

namespace GraphQL.Execution
{
    public class ExecutionContext
    {
        public ExecutionContext()
        {
            Fragments = new Fragments();
            Errors = new ExecutionErrors();
        }

        public Schema Schema { get; set; }

        public object RootObject { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public ExecutionErrors Errors { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
