using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GraphQL.Execution
{
    public class ErrorInfoProvider : IErrorInfoProvider
    {
        private static readonly ConcurrentDictionary<Type, string> _exceptionErrorCodes = new ConcurrentDictionary<Type, string>();

        private readonly ErrorInfoProviderOptions _options;

        public ErrorInfoProvider()
            : this(false)
        {
        }

        public ErrorInfoProvider(bool exposeExceptions)
            : this(new ErrorInfoProviderOptions() { ExposeExceptions = exposeExceptions })
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
            if (!string.IsNullOrWhiteSpace(executionError.Code) ||
                // skip next check to match existing functionality
                // (executionError.HasCodes) ||
                (executionError.Data?.Count > 0))
            {
                extensions = new Dictionary<string, object>();
                if (!string.IsNullOrWhiteSpace(executionError.Code))
                    extensions.Add("code", executionError.Code);
                var codes = GetCodesForError(executionError).ToList();
                if (codes.Any())
                    extensions.Add("codes", codes);
                if (executionError.Data?.Count > 0)
                    extensions.Add("data", executionError.Data);
            }

            return new ErrorInfo
            {
                Message = _options.ExposeExceptions ? executionError.ToString() : executionError.Message,
                Extensions = extensions,
            };
        }

        protected virtual IEnumerable<string> GetCodesForError(ExecutionError executionError)
        {
            // Code could be set explicitly, and not through the constructor with the exception
            if (!string.IsNullOrWhiteSpace(executionError.Code) && (executionError.InnerException == null || executionError.Code != GetErrorCode(executionError.InnerException)))
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
