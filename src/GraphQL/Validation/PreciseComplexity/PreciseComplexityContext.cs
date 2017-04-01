using System.Threading;

using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.PreciseComplexity
{
    public class PreciseComplexityContext
    {
        public PreciseComplexityContext()
        {
            Fragments = new Fragments();
        }

        public PreciseComplexityConfiguration Configuration { get; set; }

        public Document Document { get; set; }

        public ISchema Schema { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public Metrics Metrics { get; set; }
    }
}
