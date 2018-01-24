using System.Collections.Generic;
using System.Threading;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

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
        public IFieldMiddlewareBuilder FieldMiddleware { get; set; } = new FieldMiddlewareBuilder();
        public ComplexityConfiguration ComplexityConfiguration { get; set; } = null;

        public IList<IDocumentExecutionListener> Listeners { get; } = new List<IDocumentExecutionListener>();

        public IFieldNameConverter FieldNameConverter { get; set; } = new CamelCaseFieldNameConverter();

        public bool ExposeExceptions { get; set; } = false;

        //Note disabling will increase performance
        public bool EnableMetrics { get; set; } = true;

        //Note disabling will increase performance. When true all nodes will have the middleware injected for resolving fields.
        public bool SetFieldMiddleware { get; set; } = true;

    }
}
