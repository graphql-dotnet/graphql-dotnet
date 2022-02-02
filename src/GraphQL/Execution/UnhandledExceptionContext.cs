namespace GraphQL.Execution
{
    /// <summary>
    /// Provides contextual information for the unhandled exception delegate, <see cref="ExecutionContext.UnhandledExceptionDelegate"/>.
    /// </summary>
    public class UnhandledExceptionContext
    {
        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public UnhandledExceptionContext(IExecutionContext? context, IResolveFieldContext? fieldContext, Exception originalException)
        {
            Context = context;
            FieldContext = fieldContext;
            OriginalException = originalException;
            Exception = originalException;
        }

        /// <summary>
        /// Returns the execution context.
        /// </summary>
        public IExecutionContext? Context { get; }

        /// <summary>
        /// Field context whose resolver generated an error. Will be <c>null</c> if the error came from
        /// <see cref="DocumentExecuter.ExecuteAsync(ExecutionOptions)"/>, for example, validation stage.
        /// Also will be <c>null</c> between resolvers execution if <c>cancellationToken</c> is canceled.
        /// </summary>
        public IResolveFieldContext? FieldContext { get; }

        /// <summary>
        /// Original exception from field resolver or <see cref="DocumentExecuter"/>.
        /// </summary>
        public Exception OriginalException { get; }

        /// <summary>
        /// Allows to change resulting exception keeping original exception unmodified.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Allows to change resulting error message from default one.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
