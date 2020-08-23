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
        private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);

        public DocumentWriter()
            : this(indent: false)
        {
        }

        public DocumentWriter(bool indent)
            : this(BuildSerializer(indent, null, null))
        {
        }

        public DocumentWriter(bool indent, IErrorInfoProvider errorInfoProvider)
            : this(BuildSerializer(indent, null, errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider))))
        {
        }

        public DocumentWriter(IErrorInfoProvider errorInfoProvider)
            : this(false, errorInfoProvider)
        {
        }

        public DocumentWriter(Action<JsonSerializerSettings> configureSerializerSettings)
            : this(BuildSerializer(false, configureSerializerSettings ?? throw new ArgumentNullException(nameof(configureSerializerSettings)), null))
        {
        }

        public DocumentWriter(JsonSerializerSettings serializerSettings)
            : this(BuildSerializer(serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings)), null))
        {
        }

        public DocumentWriter(JsonSerializerSettings serializerSettings, IErrorInfoProvider errorInfoProvider)
            : this(BuildSerializer(
                serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings)),
                errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider))))
        {
        }

        public DocumentWriter(Action<JsonSerializerSettings> configureSerializerSettings, IErrorInfoProvider errorInfoProvider)
            : this(BuildSerializer(false,
                configureSerializerSettings ?? throw new ArgumentNullException(nameof(configureSerializerSettings)),
                errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider))))
        {
        }

        private DocumentWriter(JsonSerializer jsonSerializer)
        {
            _serializer = jsonSerializer;
        }

        private static JsonSerializerSettings GetDefaultSerializerSettings(bool indent, IErrorInfoProvider errorInfoProvider)
        {
            return new JsonSerializerSettings
            {
                Formatting = indent ? Formatting.Indented : Formatting.None,
                ContractResolver = new ExecutionResultContractResolver(errorInfoProvider ?? new ErrorInfoProvider()),
            };
        }

        private static JsonSerializer BuildSerializer(bool indent, Action<JsonSerializerSettings> configureSerializerSettings, IErrorInfoProvider errorInfoProvider)
        {
            var serializerSettings = GetDefaultSerializerSettings(indent, errorInfoProvider);
            configureSerializerSettings?.Invoke(serializerSettings);
            return BuildSerializer(serializerSettings, errorInfoProvider);
        }

        private static JsonSerializer BuildSerializer(JsonSerializerSettings serializerSettings, IErrorInfoProvider errorInfoProvider)
        {
            var serializer = JsonSerializer.CreateDefault(serializerSettings);

            if (serializerSettings.ContractResolver == null)
                serializer.ContractResolver = new ExecutionResultContractResolver(errorInfoProvider ?? new ErrorInfoProvider());
            else if (!(serializerSettings.ContractResolver is ExecutionResultContractResolver))
                throw new InvalidOperationException($"{nameof(JsonSerializerSettings.ContractResolver)} must be of type {nameof(ExecutionResultContractResolver)}");

            return serializer;
        }

        public async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var writer = new HttpResponseStreamWriter(stream, _utf8Encoding);
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
