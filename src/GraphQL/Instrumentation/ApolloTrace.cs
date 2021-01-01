using System;
using System.Collections.Generic;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Contains Apollo tracing metrics.
    /// </summary>
    public class ApolloTrace
    {
        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="start">The UTC date and time that the GraphQL document began execution.</param>
        /// <param name="durationMs">The number of milliseconds that it took to execute the GraphQL document.</param>
        public ApolloTrace(DateTime start, double durationMs)
        {
            StartTime = start;
            EndTime = start.AddMilliseconds(durationMs);
            Duration = ConvertTime(durationMs);
        }

        /// <summary>
        /// Returns the Apollo tracing version number.
        /// </summary>
        public int Version => 1;

        /// <summary>
        /// Returns the UTC date and time when the document began execution.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Returns the UTC date and time when the document completed execution.
        /// </summary>
        public DateTime EndTime { get; }

        /// <summary>
        /// Returns the duration of the document's execution, in nanoseconds.
        /// </summary>
        public long Duration { get; }

        /// <summary>
        /// Returns the parsing metrics.
        /// </summary>
        public OperationTrace Parsing { get; } = new OperationTrace();

        /// <summary>
        /// Returns the validation metrics.
        /// </summary>
        public OperationTrace Validation { get; } = new OperationTrace();

        /// <summary>
        /// Returns the execution metrics.
        /// </summary>
        public ExecutionTrace Execution { get; } = new ExecutionTrace();

        /// <summary>
        /// Converts a quantity of milliseconds to nanoseconds.
        /// </summary>
        public static long ConvertTime(double ms) => (long)(ms * 1000 * 1000);

        /// <summary>
        /// Represents the start offset and duration of an operation.
        /// </summary>
        public class OperationTrace
        {
            /// <summary>
            /// Sets or returns the start offset of the operation, in nanoseconds.
            /// </summary>
            public long StartOffset { get; set; }

            /// <summary>
            /// Sets or returns the duration of the operation, in nanoseconds.
            /// </summary>
            public long Duration { get; set; }
        }

        /// <summary>
        /// Represents metrics pertaining to the execution of a GraphQL document.
        /// </summary>
        public class ExecutionTrace
        {
            /// <summary>
            /// Returns a list of resolvers executed during the execution of a GraphQL document.
            /// </summary>
            public List<ResolverTrace> Resolvers { get; } = new List<ResolverTrace>();
        }

        /// <summary>
        /// Represents metrics pertaining to the execution of a field resolver.
        /// </summary>
        public class ResolverTrace : OperationTrace
        {
            /// <summary>
            /// Sets or returns the path of the field.
            /// </summary>
            public List<object> Path { get; set; } = new List<object>();

            /// <summary>
            /// Sets or returns the parent graph type name.
            /// </summary>
            public string ParentType { get; set; }

            /// <summary>
            /// Sets or returns the field name.
            /// </summary>
            public string FieldName { get; set; }

            /// <summary>
            /// Sets or returns the returned graph type name.
            /// </summary>
            public string ReturnType { get; set; }
        }
    }
}
