using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Introspection;
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
        
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Note if not set then standard list of validation rules will be used.
        /// </summary>
        public IEnumerable<IValidationRule> ValidationRules { get; set; }

        public IDictionary<string, object> UserContext { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Note that field middlewares apply only to an uninitialized schema. If the schema is initialized
        /// then applying different middleware through options does nothing. The schema is initialized (if not yet)
        /// at the beginning of the first call to DocumentExecuter.ExecuteAsync.
        /// </summary>
        public IFieldMiddlewareBuilder FieldMiddleware { get; set; } = new FieldMiddlewareBuilder();

        public ComplexityConfiguration ComplexityConfiguration { get; set; }

        public IList<IDocumentExecutionListener> Listeners { get; } = new List<IDocumentExecutionListener>();

        public IFieldNameConverter FieldNameConverter { get; set; } = CamelCaseFieldNameConverter.Instance;

        public bool ExposeExceptions { get; set; }

        /// <summary>
        /// This setting essentially allows Apollo Tracing. Disabling will increase performance.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        public bool ThrowOnUnhandledException { get; set; }

        /// <summary>
        /// Allows to override, hide, modify or just log the unhandled exception before wrap it into ExecutionError.
        /// This can be useful for hiding error messages that reveal server implementation details.
        /// </summary>
        public Action<UnhandledExceptionContext> UnhandledExceptionDelegate { get; set; } = context => { };

        /// <summary>
        /// Provides the ability to filter the schema upon introspection to hide types.
        /// </summary>
        public ISchemaFilter SchemaFilter { get; set; } = new DefaultSchemaFilter();

        public IServiceProvider Services { get; set; }
    }
}
