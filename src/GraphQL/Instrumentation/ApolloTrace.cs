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
        /// <param name="start">The date and time that the GraphQL document began execution. If not UTC, this value will be converted to UTC.</param>
        /// <param name="durationMs">The number of milliseconds that it took to execute the GraphQL document.</param>
        public ApolloTrace(DateTime start, double durationMs)
        {
            StartTime = start.ToUniversalTime();
            EndTime = StartTime.AddMilliseconds(durationMs);
            Duration = ConvertTime(durationMs);
        }

        /// <summary>
        /// Returns the Apollo tracing version number.
        /// </summary>
        public int Version => 1;

        /// <summary>
        /// Returns the UTC date and time when the document began execution. Should be serialized as a RFC 3339 string.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Returns the UTC date and time when the document completed execution. Should be serialized as a RFC 3339 string.
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
        internal static long ConvertTime(double ms) => (long)(ms * 1000 * 1000);

        /// <summary>
        /// Represents the start offset and duration of an operation.
        /// </summary>
        public class OperationTrace
        {
            /// <summary>
            /// Gets or sets the start offset of the operation, in nanoseconds.
            /// </summary>
            public long StartOffset { get; set; }

            /// <summary>
            /// Gets or sets the duration of the operation, in nanoseconds.
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
            /// Gets or sets the path of the field.
            /// </summary>
            public List<object>? Path { get; set; }

            /// <summary>
            /// Gets or sets the parent graph type name.
            /// </summary>
            public string? ParentType { get; set; }

            /// <summary>
            /// Gets or sets the field name.
            /// </summary>
            public string? FieldName { get; set; }

            /// <summary>
            /// Gets or sets the returned graph type name.
            /// </summary>
            public string? ReturnType { get; set; }
        }
    }
}
