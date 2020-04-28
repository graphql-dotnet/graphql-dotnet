using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphQL.Execution
{
    public class ErrorParser : IErrorParser
    {
        private readonly ErrorParserOptions _options;

        public ErrorParser()
            : this(false)
        {
        }

        public ErrorParser(bool exposeExceptions)
            : this(new ErrorParserOptions() { ExposeExceptions = exposeExceptions })
        {
        }

        public ErrorParser(ErrorParserOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public ErrorParser(Action<ErrorParserOptions> optionsBuilder)
        {
            if (optionsBuilder == null)
                throw new ArgumentNullException(nameof(optionsBuilder));
            _options = new ErrorParserOptions();
            optionsBuilder(_options);
        }

        public ParsedError Parse(ExecutionError executionError)
        {
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
                if (executionError.Data != null)
                    extensions.Add("data", executionError.Data);
            }

            return new ParsedError()
            {
                Message = _options.ExposeExceptions ? executionError.ToString() : executionError.Message,
                Locations = executionError.Locations,
                Path = executionError.Path,
                Extensions = extensions,
            };
        }
    }
}
