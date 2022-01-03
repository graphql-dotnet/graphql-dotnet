using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// Serializes an <see cref="ExecutionResult"/> (or any other object) to a stream using
    /// the <see cref="Newtonsoft.Json"/> library.
    /// </summary>
    public class GraphQLSerializer : IGraphQLSerializer, IGraphQLTextSerializer
    {
        private readonly JsonArrayPool _jsonArrayPool = new JsonArrayPool(ArrayPool<char>.Shared);
        private readonly JsonSerializer _serializer;
        private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with default settings:
        /// no indenting and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        public GraphQLSerializer()
            : this(indent: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings
        /// and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="indent">Indicates if child objects should be indented</param>
        public GraphQLSerializer(bool indent)
            : this(BuildSerializer(indent, null, null))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings.
        /// </summary>
        /// <param name="indent">Indicates if child objects should be indented</param>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public GraphQLSerializer(bool indent, IErrorInfoProvider errorInfoProvider)
            : this(BuildSerializer(indent, null, errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider))))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with no indenting and the
        /// specified <see cref="IErrorInfoProvider"/>.
        /// </summary>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public GraphQLSerializer(IErrorInfoProvider errorInfoProvider)
            : this(false, errorInfoProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class configured with the specified callback.
        /// Configuration defaults to no indenting and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="configureSerializerSettings">Specifies a callback used to configure the JSON serializer</param>
        public GraphQLSerializer(Action<JsonSerializerSettings> configureSerializerSettings)
            : this(BuildSerializer(false, configureSerializerSettings ?? throw new ArgumentNullException(nameof(configureSerializerSettings)), null))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings
        /// and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="serializerSettings">Specifies the JSON serializer settings</param>
        public GraphQLSerializer(JsonSerializerSettings serializerSettings)
            : this(BuildSerializer(serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings)), null))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings.
        /// </summary>
        /// <param name="serializerSettings">Specifies the JSON serializer settings</param>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public GraphQLSerializer(JsonSerializerSettings serializerSettings, IErrorInfoProvider errorInfoProvider)
            : this(BuildSerializer(
                serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings)),
                errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider))))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings.
        /// Configuration defaults to no indenting and the specified instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="configureSerializerSettings">Specifies a callback used to configure the JSON serializer</param>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public GraphQLSerializer(Action<JsonSerializerSettings> configureSerializerSettings, IErrorInfoProvider errorInfoProvider)
            : this(BuildSerializer(false,
                configureSerializerSettings ?? throw new ArgumentNullException(nameof(configureSerializerSettings)),
                errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider))))
        {
        }

        private GraphQLSerializer(JsonSerializer jsonSerializer)
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

        /// <inheritdoc/>
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

        /// <inheritdoc cref="IGraphQLTextSerializer.Write{T}(T)"/>
        public void Write<T>(TextWriter textWriter, T value)
        {
            using var stringWriter = new JsonTextWriter(textWriter);
            _serializer.Serialize(stringWriter, value);
        }

        /// <inheritdoc/>
        public string Write<T>(T value)
        {
            using var stringWriter = new StringWriter();
            Write(stringWriter, value);
            return stringWriter.ToString();
        }

        /// <inheritdoc/>
        public ValueTask<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            //note: do not dispose of stringReader or else the underlying stream will be disposed
            var stringReader = new StreamReader(stream, Encoding.UTF8);
            using var jsonReader = new JsonTextReader(stringReader);
            return new ValueTask<T>(_serializer.Deserialize<T>(jsonReader));
        }

        /// <summary>
        /// Deserializes the specified string to the specified object type.
        /// </summary>
        public T Read<T>(TextReader json)
        {
            using var jsonReader = new JsonTextReader(json);
            return _serializer.Deserialize<T>(jsonReader);
        }

        /// <inheritdoc/>
        public T Read<T>(string json)
            => json == null ? default : Read<T>(new StringReader(json));

        /// <summary>
        /// Converts the <see cref="JObject"/> representing a single JSON value into a <typeparamref name="T"/>.
        /// A <paramref name="jObject"/> of <see langword="null"/> returns <see langword="default"/>.
        /// </summary>
        public T ReadNode<T>(JObject jObject)
            => jObject == null ? default : jObject.ToObject<T>(_serializer);

        T IGraphQLSerializer.ReadNode<T>(object value)
            => ReadNode<T>((JObject)value);
    }
}
