using GraphQL.Language;
using GraphQL.Types;
using System.Threading;
using GraphQL.Language.AST;

namespace GraphQL.Execution
{
    public class ExecutionContext
    {
        public ExecutionContext()
        {
            Fragments = new Fragments();
            Errors = new ExecutionErrors();
        }

        public ISchema Schema { get; set; }

        public object RootValue { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public ExecutionErrors Errors { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
