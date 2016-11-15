using System.Collections.Generic;
using System.Threading;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL
{
    public class ExecutionOptions
    {
        public ISchema Schema { get; set; }
        public object Root { get; set; }
        public string Query { get; set; }
        public string OperationName { get; set; }
        public Document Document { get; set; }
        public Inputs Inputs { get; set; }
        public CancellationToken CancellationToken { get; set; } = default(CancellationToken);
        public IEnumerable<IValidationRule> ValidationRules { get; set; }
        public object UserContext { get; set; }
        public readonly IList<IDocumentExecutionListener> Listeners = new List<IDocumentExecutionListener>();
        public IFieldMiddlewareBuilder FieldMiddleware { get; set; } = new FieldMiddlewareBuilder();
    }
}
