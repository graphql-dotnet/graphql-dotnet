using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    public class DocumentWriter : IDocumentWriter
    {
        private readonly JsonArrayPool _jsonArrayPool = new JsonArrayPool(ArrayPool<char>.Shared);
        private readonly JsonSerializer _serializer;
        private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

        public DocumentWriter()
            : this(indent: false)
        {
        }

        public DocumentWriter(bool indent)
            : this(GetDefaultSerializerSettings(indent, null))
        {
        }

        public DocumentWriter(bool indent, IErrorParser errorParser)
            : this(GetDefaultSerializerSettings(indent, errorParser ?? throw new ArgumentNullException(nameof(errorParser))))
        {
        }

        public DocumentWriter(IErrorParser errorParser)
            : this(false, errorParser)
        {
        }

        public DocumentWriter(Action<JsonSerializerSettings> configureSerializerSettings)
        {
            var serializerSettings = GetDefaultSerializerSettings(indent: false, errorParser: null);
            configureSerializerSettings?.Invoke(serializerSettings);

            _serializer = BuildSerializer(serializerSettings);
        }

        public DocumentWriter(JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings == null)
                throw new ArgumentNullException(nameof(serializerSettings));

            _serializer = BuildSerializer(serializerSettings);
        }

        private static JsonSerializerSettings GetDefaultSerializerSettings(bool indent, IErrorParser errorParser)
        {
            return new JsonSerializerSettings {
                Formatting = indent ? Formatting.Indented : Formatting.None,
                ContractResolver = new ExecutionResultContractResolver(errorParser ?? new ErrorParser()),
            };
        }

        private JsonSerializer BuildSerializer(JsonSerializerSettings serializerSettings)
        {
            var serializer = JsonSerializer.CreateDefault(serializerSettings);

            if (serializerSettings.ContractResolver == null)
                serializer.ContractResolver = new ExecutionResultContractResolver(new ErrorParser());

            return serializer;
        }

        public async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var writer = new HttpResponseStreamWriter(stream, Utf8Encoding);
            using var jsonWriter = new JsonTextWriter(writer)
            {
                ArrayPool = _jsonArrayPool,
                CloseOutput = false,
                AutoCompleteOnClose = false
            };
            
            _serializer.Serialize(jsonWriter, value);
            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
