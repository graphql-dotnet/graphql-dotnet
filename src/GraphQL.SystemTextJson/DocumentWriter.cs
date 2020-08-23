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

        public DocumentWriter()
            : this(indent: false)
        {
        }

        public DocumentWriter(bool indent)
            : this(GetDefaultSerializerOptions(indent))
        {
        }

        public DocumentWriter(bool indent, IErrorInfoProvider errorInfoProvider)
            : this(GetDefaultSerializerOptions(indent), errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider)))
        {
        }

        public DocumentWriter(IErrorInfoProvider errorInfoProvider)
            : this(false, errorInfoProvider)
        {
        }

        public DocumentWriter(Action<JsonSerializerOptions> configureSerializerOptions)
            : this(configureSerializerOptions, null)
        {
        }

        public DocumentWriter(JsonSerializerOptions serializerOptions)
            : this(serializerOptions, null)
        {
        }

        public DocumentWriter(JsonSerializerOptions serializerOptions, IErrorInfoProvider errorInfoProvider)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            // TODO: fix this: it modifies serializerOptions
            ConfigureOptions(errorInfoProvider);
        }

        public DocumentWriter(Action<JsonSerializerOptions> configureSerializerOptions, IErrorInfoProvider errorInfoProvider)
        {
            _options = GetDefaultSerializerOptions(indent: false);
            configureSerializerOptions?.Invoke(_options);

            ConfigureOptions(errorInfoProvider);
        }

        private void ConfigureOptions(IErrorInfoProvider errorInfoProvider)
        {
            if (!_options.Converters.Any(c => c.CanConvert(typeof(ExecutionResult))))
            {
                _options.Converters.Add(new ExecutionResultJsonConverter(errorInfoProvider ?? new ErrorInfoProvider()));
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
