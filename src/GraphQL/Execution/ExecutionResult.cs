using System;
using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;

namespace GraphQL
{
    /// <summary>
    /// Represents the result of an execution.
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Returns the data from the graph resolvers. This property is serialized as part of the GraphQL json response.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Returns a set of errors that occurred during any stage of processing (parsing, validating, executing, etc.). This property is serialized as part of the GraphQL json response.
        /// </summary>
        public ExecutionErrors Errors { get; set; }

        /// <summary>
        /// Returns the original GraphQL query.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Returns the parsed GraphQL request.
        /// </summary>
        public Document Document { get; set; }

        /// <summary>
        /// Returns the GraphQL operation that is being executed.
        /// </summary>
        public Operation Operation { get; set; }

        /// <summary>
        /// Returns the performance metrics (Apollo Tracing) when enabled by <see cref="ExecutionOptions.EnableMetrics"/>.
        /// </summary>
        public PerfRecord[] Perf { get; set; }

        /// <summary>
        /// Returns additional user-defined data; see <see cref="IExecutionContext.Extensions"/> and <see cref="IResolveFieldContext.Extensions"/>. This property is serialized as part of the GraphQL json response.
        /// </summary>
        public Dictionary<string, object> Extensions { get; set; }

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
    }
}
