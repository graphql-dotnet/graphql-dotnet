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
    /// <summary>Configuration options to be passed to <see cref="IDocumentExecuter"/> to execute a query</summary>
    public class ExecutionOptions : IProvideUserContext
    {
        /// <summary>
        /// Schema of graph to use; required
        /// <br/><br/>
        /// Schema will be initialized if it has not yet been initialized.
        /// </summary>
        public ISchema Schema { get; set; }

        /// <summary>Object to pass to the <see cref="IResolveFieldContext.Source"/> property of first-level resolvers</summary>
        public object Root { get; set; }

        /// <summary>GraphQL query to parse and execute; required</summary>
        public string Query { get; set; }

        /// <summary>GraphQL query operation name; optional, defaults to first (if any) operation defined in query</summary>
        public string OperationName { get; set; }

        /// <summary>Parsed GraphQL request; can be used to increase performance when implementing a cache of parsed GraphQL requests (a <see cref="Language.AST.Document"/>). If not set, it will be parsed from <see cref="Query"/></summary>
        public Document Document { get; set; }

        /// <summary>Input variables to GraphQL request</summary>
        public Inputs Inputs { get; set; }

        /// <summary><see cref="System.Threading.CancellationToken">CancellationToken</see> to cancel the request at any stage of its execution; defaults to <see cref="System.Threading.CancellationToken.None"/></summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>Validation rules to be used by the <see cref="IDocumentValidator"/>; defaults to standard list of of validation rules - see <see cref="DocumentValidator.CoreRules"/></summary>
        public IEnumerable<IValidationRule> ValidationRules { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, object> UserContext { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Note that field middlewares apply only to an uninitialized schema. If the schema is initialized
        /// then applying different middleware through options does nothing. The schema is initialized (if not yet)
        /// at the beginning of the first call to <see cref="DocumentExecuter"/>.<see cref="DocumentExecuter.ExecuteAsync(ExecutionOptions)">ExecuteAsync</see>.
        /// </summary>
        public IFieldMiddlewareBuilder FieldMiddleware { get; set; } = new FieldMiddlewareBuilder();

        /// <summary>Complexity constraints for <see cref="IComplexityAnalyzer"/> to use to validate maximum query complexity</summary>
        public ComplexityConfiguration ComplexityConfiguration { get; set; }

        /// <summary>A list of <see cref="IDocumentExecutionListener"/>s, enabling code to be executed at various points during the processing of the GraphQL query</summary>
        public List<IDocumentExecutionListener> Listeners { get; } = new List<IDocumentExecutionListener>();

        /// <summary>Field and argument names are sanitized by the provided <see cref="INameConverter"/>; defaults to <see cref="CamelCaseNameConverter"/></summary>
        public INameConverter NameConverter { get; set; } = CamelCaseNameConverter.Instance;

        /// <summary>This setting essentially allows Apollo Tracing. Disabling will increase performance.</summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>When false, captures unhandled exceptions and returns them within <see cref="ExecutionResult.Errors">ExecutionResult.Errors</see></summary>
        public bool ThrowOnUnhandledException { get; set; }

        /// <summary>
        /// A delegate that can override, hide, modify, or log unhandled exceptions before they are stored
        /// within <see cref="ExecutionResult.Errors"/> as an <see cref="ExecutionError"/>.
        /// This can be useful for hiding error messages that reveal server implementation details.
        /// </summary>
        public Action<UnhandledExceptionContext> UnhandledExceptionDelegate { get; set; } = context => { };

        /// <summary>If set, limits the maximum number of nodes executed in parallel</summary>
        public int? MaxParallelExecutionCount { get; set; }

        /// <summary>Provides the ability to filter the schema upon introspection to hide types; by default no types are hidden.</summary>
        public ISchemaFilter SchemaFilter { get; set; } = new DefaultSchemaFilter();

        /// <summary>
        /// The service provider for the executing request. Typically this is set to a scoped service provider
        /// from your dependency injection framework.
        /// </summary>
        public IServiceProvider RequestServices { get; set; }
    }
}
