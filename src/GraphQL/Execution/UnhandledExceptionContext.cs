using GraphQL.Types;
using System;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides contextual information for the unhandled exception delegate, <see cref="ExecutionContext.UnhandledExceptionDelegate"/>.
    /// </summary>
    public class UnhandledExceptionContext
    {
        public UnhandledExceptionContext(ExecutionContext context, IResolveFieldContext fieldContext, Exception originalException)
        {
            Context = context;
            FieldContext = fieldContext;
            OriginalException = originalException;
            Exception = originalException;
        }

        public ExecutionContext Context { get; }

        /// <summary>
        /// Field context whose resolver generated an error. Can be null if the error came from DocumentExecuter.
        /// </summary>
        public IResolveFieldContext FieldContext { get; }

        /// <summary>
        /// Original exception from field resolver or DocumentExecuter.
        /// </summary>
        public Exception OriginalException { get; }

        /// <summary>
        /// Allows to change resulting exception keeping original exception unmodified.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Allows to change resulting error message from default one.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
