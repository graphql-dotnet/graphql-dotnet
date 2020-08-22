using System;
using System.Collections.Generic;

namespace GraphQL.Execution
{
    public class ErrorInfoProvider : IErrorInfoProvider
    {
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
                if (executionError.HasCodes)
                    extensions.Add("codes", executionError.Codes);
                if (executionError.Data?.Count > 0)
                    extensions.Add("data", executionError.Data);
            }

            return new ErrorInfo
            {
                Message = _options.ExposeExceptions ? executionError.ToString() : executionError.Message,
                Extensions = extensions,
            };
        }
    }
}
