using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQLParser;
using GraphQLParser.AST;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL
{
    /// <summary>
    /// Represents the result of an execution.
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Indicates if the operation included execution. If an error was encountered BEFORE execution begins,
        /// the data entry SHOULD NOT be present in the result. If an error was encountered DURING the execution
        /// that prevented a valid response, the data entry in the response SHOULD BE <see langword="null"/>.
        /// </summary>
        public bool Executed { get; set; }

        /// <summary>
        /// Returns the data from the graph resolvers. This property is serialized as part of the GraphQL json response.
        /// Should be set to <see langword="null"/> for subscription results.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of returned subscription fields along with their
        /// response streams as <see cref="IObservable{T}"/> implementations.
        /// Should be set to <see langword="null"/> for query or mutation results.
        /// According to the GraphQL specification this dictionary should have exactly one item.
        /// </summary>
        public IDictionary<string, IObservable<ExecutionResult>>? Streams { get; set; }

        /// <summary>
        /// Returns a set of errors that occurred during any stage of processing (parsing, validating, executing, etc.). This property is serialized as part of the GraphQL json response.
        /// </summary>
        public ExecutionErrors? Errors { get; set; }

        /// <summary>
        /// Returns the original GraphQL query.
        /// </summary>
        public ROM Query { get; set; }

        /// <summary>
        /// Returns the parsed GraphQL request.
        /// </summary>
        public GraphQLDocument? Document { get; set; }

        /// <summary>
        /// Returns the GraphQL operation that is being executed.
        /// </summary>
        public GraphQLOperationDefinition? Operation { get; set; }

        /// <summary>
        /// Returns the performance metrics (Apollo Tracing) when enabled by <see cref="ExecutionOptions.EnableMetrics"/>.
        /// </summary>
        public PerfRecord[]? Perf { get; set; }

        /// <summary>
        /// Returns additional user-defined data; see <see cref="IExecutionContext.OutputExtensions"/> and <see cref="IResolveFieldContext.OutputExtensions"/>. This property is serialized as part of the GraphQL json response.
        /// </summary>
        public Dictionary<string, object?>? Extensions { get; set; }

        /// <summary>
        /// Initializes a new instance with all properties set to their defaults.
        /// </summary>
        public ExecutionResult()
        {
        }

        /// <summary>
        /// Initializes a new instance as a clone of an existing <see cref="ExecutionResult"/>.
        /// </summary>
        public ExecutionResult(ExecutionResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            Data = result.Data;
            Errors = result.Errors;
            Query = result.Query;
            Operation = result.Operation;
            Document = result.Document;
            Perf = result.Perf;
            Extensions = result.Extensions;
        }

        /// <summary>
        /// Initializes a new instance with the <see cref="Query"/>, <see cref="Document"/>,
        /// <see cref="Operation"/> and <see cref="Extensions"/> properties set from the
        /// specified <see cref="ExecutionContext"/>.
        /// </summary>
        internal ExecutionResult(ExecutionContext context)
        {
            Query = context.Document.Source;
            Document = context.Document;
            Operation = context.Operation;
            Extensions = context.OutputExtensions;
        }

        /// <summary>
        /// Adds the specified error to <see cref="Errors"/>.
        /// </summary>
        /// <returns>Reference to this.</returns>
        public ExecutionResult AddError(ExecutionError error)
        {
            (Errors ??= new()).Add(error);
            return this;
        }

        /// <summary>
        /// Adds errors from the specified <see cref="ExecutionErrors"/> to <see cref="Errors"/>.
        /// </summary>
        /// <param name="errors">List of execution errors.</param>
        /// <returns>Reference to this.</returns>
        public ExecutionResult AddErrors(ExecutionErrors errors)
        {
            if (errors?.Count > 0)
            {
                Errors ??= new(errors.Count);

                foreach (var error in errors.List!)
                    Errors.Add(error);
            }

            return this;
        }
    }
}
