using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GraphQL.Validation;

namespace GraphQL.Execution
{
    public class ErrorInfoProvider : IErrorInfoProvider
    {
        private static readonly ConcurrentDictionary<Type, string> _exceptionErrorCodes = new ConcurrentDictionary<Type, string>();

        private readonly ErrorInfoProviderOptions _options;

        public ErrorInfoProvider()
            : this(new ErrorInfoProviderOptions())
        {
        }

        public ErrorInfoProvider(ErrorInfoProviderOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public ErrorInfoProvider(Action<ErrorInfoProviderOptions> optionsBuilder)
        {
            if (optionsBuilder == null)
                throw new ArgumentNullException(nameof(optionsBuilder));
            _options = new ErrorInfoProviderOptions();
            optionsBuilder(_options);
        }

        public virtual ErrorInfo GetInfo(ExecutionError executionError)
        {
            if (executionError == null)
                throw new ArgumentNullException(nameof(executionError));

            IDictionary<string, object> extensions = null;

            if (_options.ExposeExtensions)
            {
                var code = _options.ExposeCode ? executionError.Code : null;
                var codes = _options.ExposeCodes ? GetCodesForError(executionError).ToList() : null;
                if (codes?.Count == 0)
                    codes = null;
                var data = _options.ExposeData && executionError.Data?.Count > 0 ? executionError.Data : null;

                if (code != null || codes != null || data != null)
                {
                    extensions = new Dictionary<string, object>();
                    if (code != null)
                    {
                        extensions.Add("code", code);
                        if (executionError is ValidationError validationError)
                            extensions.Add("number", validationError.Number);
                    }
                    if (codes != null)
                        extensions.Add("codes", codes);
                    if (data != null)
                        extensions.Add("data", data);
                }
            }

            return new ErrorInfo
            {
                Message = _options.ExposeExceptionStackTrace ? executionError.ToString() : executionError.Message,
                Extensions = extensions,
            };
        }

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

            return GetAllCapsRepresentation(code);
        }

        private static string GetAllCapsRepresentation(string str)
        {
            return Regex
                .Replace(NormalizeString(str), @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4")
                .ToUpperInvariant();
        }

        private static string NormalizeString(string str)
        {
            str = str?.Trim();
            return string.IsNullOrWhiteSpace(str)
                ? string.Empty
                : NormalizeTypeName(str);
        }

        private static string NormalizeTypeName(string name)
        {
            var tickIndex = name.IndexOf('`');
            return tickIndex >= 0
                ? name.Substring(0, tickIndex)
                : name;
        }
    }
}
