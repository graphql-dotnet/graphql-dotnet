using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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

        public DocumentWriter(Action<JsonSerializerOptions> configureSerializerOptions)
        {
            _options = GetDefaultSerializerOptions(indent: false);
            configureSerializerOptions?.Invoke(_options);

            ConfigureOptions();
        }

        public DocumentWriter(JsonSerializerOptions serializerOptions)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            ConfigureOptions();
        }

        private void ConfigureOptions()
        {
            if (!_options.Converters.Any(c => c.CanConvert(typeof(ExecutionResult))))
            {
                _options.Converters.Add(new ExecutionResultJsonConverter());
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
