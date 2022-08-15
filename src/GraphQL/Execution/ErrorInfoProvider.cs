using System.Collections.Concurrent;
using GraphQL.Validation;

namespace GraphQL.Execution
{
    /// <inheritdoc cref="IErrorInfoProvider"/>
    public class ErrorInfoProvider : IErrorInfoProvider
    {
        private static readonly ConcurrentDictionary<Type, string> _exceptionErrorCodes = new();

        private readonly ErrorInfoProviderOptions _options;

        /// <summary>
        /// Initializes an <see cref="ErrorInfoProvider"/> with a default set of <see cref="ErrorInfoProviderOptions"/>.
        /// </summary>
        public ErrorInfoProvider()
            : this(new ErrorInfoProviderOptions())
        {
        }

        /// <summary>
        /// Initializes an <see cref="ErrorInfoProvider"/> with a specified set of <see cref="ErrorInfoProviderOptions"/>.
        /// </summary>
        public ErrorInfoProvider(ErrorInfoProviderOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Initializes an <see cref="ErrorInfoProvider"/> with a set of <see cref="ErrorInfoProviderOptions"/> filled out by the specified delegate.
        /// </summary>
        public ErrorInfoProvider(Action<ErrorInfoProviderOptions> optionsBuilder)
        {
            if (optionsBuilder == null)
                throw new ArgumentNullException(nameof(optionsBuilder));
            _options = new ErrorInfoProviderOptions();
            optionsBuilder(_options);
        }

        /// <inheritdoc/>
        public virtual ErrorInfo GetInfo(ExecutionError executionError)
        {
            if (executionError == null)
                throw new ArgumentNullException(nameof(executionError));

            IDictionary<string, object?>? extensions = null;

            if (_options.ExposeExtensions)
            {
                var code = _options.ExposeCode ? executionError.Code : null;
                var codes = _options.ExposeCodes ? GetCodesForError(executionError).ToList() : null;
                if (codes?.Count == 0)
                    codes = null;
                var number = _options.ExposeCode && executionError is ValidationError validationError ? validationError.Number : null;
                var data = _options.ExposeData && executionError.Data?.Count > 0 ? executionError.Data : null;
                var details = _options.ExposeExceptionDetails && _options.ExposeExceptionDetailsMode == ExposeExceptionDetailsMode.Extensions
                    ? executionError.ToString()
                    : null;

                if (code != null || codes != null || data != null || details != null)
                {
                    extensions = new Dictionary<string, object?>();
                    if (code != null)
                        extensions.Add("code", code);
                    if (codes != null)
                        extensions.Add("codes", codes);
                    if (number != null)
                        extensions.Add("number", number);
                    if (data != null)
                        extensions.Add("data", data);
                    if (details != null)
                        extensions.Add("details", details);
                }
            }

            return new ErrorInfo
            {
                Message = _options.ExposeExceptionDetails && _options.ExposeExceptionDetailsMode == ExposeExceptionDetailsMode.Message ? executionError.ToString() : executionError.Message,
                Extensions = extensions,
            };
        }

        /// <summary>
        /// <para>Returns a list of error codes derived from a specified <see cref="ExecutionError"/> instance.</para>
        /// <para>
        /// By default, this returns the <see cref="ExecutionError.Code"/> value if set, along with
        /// codes generated from the type of the <see cref="Exception.InnerException"/> and all their inner exceptions.
        /// </para>
        /// </summary>
        protected virtual IEnumerable<string> GetCodesForError(ExecutionError executionError)
        {
            // Code could be set explicitly, and not through the constructor with the exception
            if (executionError.Code != null && (executionError.InnerException == null || executionError.Code != GetErrorCode(executionError.InnerException)))
                yield return executionError.Code;

            var current = executionError.InnerException;

            while (current != null)
            {
                yield return GetErrorCode(current);
                current = current.InnerException;
            }
        }

        /// <summary>
        /// Generates an normalized error code for the specified exception by taking the type name, removing the "GraphQL" prefix, if any,
        /// removing the "Exception" suffix, if any, and then converting the result from PascalCase to UPPER_CASE.
        /// </summary>
        public static string GetErrorCode(Type exceptionType) => _exceptionErrorCodes.GetOrAdd(exceptionType, NormalizeErrorCode);

        /// <summary>
        /// Generates an normalized error code for the specified exception by taking the type name, removing the "GraphQL" prefix, if any,
        /// removing the "Exception" suffix, if any, and then converting the result from PascalCase to UPPER_CASE.
        /// </summary>
        public static string GetErrorCode<T>() where T : Exception => GetErrorCode(typeof(T));

        /// <summary>
        /// Generates an normalized error code for the specified exception by taking the type name, removing the "GraphQL" prefix, if any,
        /// removing the "Exception" suffix, if any, and then converting the result from PascalCase to UPPER_CASE.
        /// </summary>
        public static string GetErrorCode(Exception exception) => GetErrorCode(exception.GetType());

        private static string NormalizeErrorCode(Type exceptionType)
        {
            var code = exceptionType.Name;

            if (code.EndsWith(nameof(Exception), StringComparison.InvariantCulture))
            {
                code = code.Substring(0, code.Length - nameof(Exception).Length);
            }

            if (code.StartsWith("GraphQL", StringComparison.InvariantCulture))
            {
                code = code.Substring("GraphQL".Length);
            }

            var tickIndex = code.IndexOf('`');
            if (tickIndex >= 0)
            {
                code = code.Substring(0, tickIndex);
            }

            return code.ToConstantCase();
        }
    }
}
