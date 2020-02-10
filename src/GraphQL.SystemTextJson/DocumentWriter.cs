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
            : this(new JsonSerializerOptions { WriteIndented = indent, PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        {
        }

        public DocumentWriter(JsonSerializerOptions options)
        {
            _options = options;

            if (!_options.Converters.Any(c => c.CanConvert(typeof(ExecutionResult))))
            {
                _options.Converters.Add(new ExecutionResultJsonConverter());
            }
        }

        public Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
            => JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken);
    }
}
