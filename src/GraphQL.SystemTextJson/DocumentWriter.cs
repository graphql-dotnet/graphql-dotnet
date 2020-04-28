using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;

namespace GraphQL.SystemTextJson
{
    public class DocumentWriter : IDocumentWriter
    {
        private readonly JsonSerializerOptions _options;
        private readonly IErrorParser _errorParser;

        public DocumentWriter()
            : this(indent: false)
        {
        }

        public DocumentWriter(bool indent)
            : this(GetDefaultSerializerOptions(indent))
        {
        }

        public DocumentWriter(bool indent, IErrorParser errorParser)
            : this(GetDefaultSerializerOptions(indent), errorParser ?? throw new ArgumentNullException(nameof(errorParser)))
        {
            _errorParser = errorParser;
        }

        public DocumentWriter(IErrorParser errorParser)
            : this(false, errorParser)
        {
        }

        public DocumentWriter(Action<JsonSerializerOptions> configureSerializerOptions)
        {
            _options = GetDefaultSerializerOptions(indent: false);
            configureSerializerOptions?.Invoke(_options);

            ConfigureOptions(null);
        }

        public DocumentWriter(JsonSerializerOptions serializerOptions)
            : this(serializerOptions, null)
        {
        }

        private DocumentWriter(JsonSerializerOptions serializerOptions, IErrorParser errorParser)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            ConfigureOptions(errorParser);
        }

        private void ConfigureOptions(IErrorParser errorParser)
        {
            if (!_options.Converters.Any(c => c.CanConvert(typeof(ExecutionResult))))
            {
                _options.Converters.Add(new ExecutionResultJsonConverter(errorParser ?? new ErrorParser()));
            }

            if (!_options.Converters.Any(c => c.CanConvert(typeof(JsonConverterBigInteger))))
            {
                _options.Converters.Add(new JsonConverterBigInteger());
            }
        }

        private static JsonSerializerOptions GetDefaultSerializerOptions(bool indent)
            => new JsonSerializerOptions { WriteIndented = indent, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
            => JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken);
    }
}
