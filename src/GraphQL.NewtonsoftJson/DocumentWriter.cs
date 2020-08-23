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
            : this(GetDefaultSerializerSettings(indent, null))
        {
        }

        public DocumentWriter(bool indent, IErrorInfoProvider errorInfoProvider)
            : this(GetDefaultSerializerSettings(indent, errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider))))
        {
        }

        public DocumentWriter(IErrorInfoProvider errorInfoProvider)
            : this(false, errorInfoProvider)
        {
        }

        public DocumentWriter(Action<JsonSerializerSettings> configureSerializerSettings)
        {
            var serializerSettings = GetDefaultSerializerSettings(indent: false, errorInfoProvider: null);
            configureSerializerSettings?.Invoke(serializerSettings);

            _serializer = BuildSerializer(serializerSettings);
        }

        public DocumentWriter(JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings == null)
                throw new ArgumentNullException(nameof(serializerSettings));

            _serializer = BuildSerializer(serializerSettings);
        }

        public DocumentWriter(JsonSerializerSettings serializerSettings, IErrorInfoProvider errorInfoProvider)
        {
            if (serializerSettings == null)
                throw new ArgumentNullException(nameof(serializerSettings));

            if (errorInfoProvider == null)
                throw new ArgumentNullException(nameof(errorInfoProvider));

            if (serializerSettings.ContractResolver != null)
                throw new InvalidOperationException($"{nameof(serializerSettings)}.{nameof(JsonSerializerSettings.ContractResolver)} must be null to use this constructor");

            serializerSettings.ContractResolver = new ExecutionResultContractResolver(errorInfoProvider);

            _serializer = BuildSerializer(serializerSettings);
        }

        private static JsonSerializerSettings GetDefaultSerializerSettings(bool indent, IErrorInfoProvider errorInfoProvider)
        {
            return new JsonSerializerSettings {
                Formatting = indent ? Formatting.Indented : Formatting.None,
                ContractResolver = new ExecutionResultContractResolver(errorInfoProvider ?? new ErrorInfoProvider()),
            };
        }

        private JsonSerializer BuildSerializer(JsonSerializerSettings serializerSettings)
        {
            var serializer = JsonSerializer.CreateDefault(serializerSettings);

            if (serializerSettings.ContractResolver == null)
                serializer.ContractResolver = new ExecutionResultContractResolver(new ErrorInfoProvider());

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
