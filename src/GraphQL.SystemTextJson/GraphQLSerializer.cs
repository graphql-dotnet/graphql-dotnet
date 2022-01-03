using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Transports.Json;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// Serializes an <see cref="ExecutionResult"/> (or any other object) to a stream using
    /// the <see cref="System.Text.Json"/> library.
    /// </summary>
    public class GraphQLSerializer : IGraphQLSerializer, IGraphQLTextSerializer
    {
        private readonly JsonSerializerOptions _options;

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
            : this(GetDefaultSerializerOptions(indent))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings.
        /// </summary>
        /// <param name="indent">Indicates if child objects should be indented</param>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public GraphQLSerializer(bool indent, IErrorInfoProvider errorInfoProvider)
            : this(GetDefaultSerializerOptions(indent), errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider)))
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
        /// <param name="configureSerializerOptions">Specifies a callback used to configure the JSON serializer</param>
        public GraphQLSerializer(Action<JsonSerializerOptions> configureSerializerOptions)
        {
            if (configureSerializerOptions == null)
                throw new ArgumentNullException(nameof(configureSerializerOptions));

            _options = GetDefaultSerializerOptions(indent: false);
            configureSerializerOptions.Invoke(_options);

            ConfigureOptions(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings
        /// and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="serializerOptions">Specifies the JSON serializer settings</param>
        public GraphQLSerializer(JsonSerializerOptions serializerOptions)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            // TODO: fix this: it modifies serializerOptions
            ConfigureOptions(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings.
        /// </summary>
        /// <param name="serializerOptions">Specifies the JSON serializer settings</param>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public GraphQLSerializer(JsonSerializerOptions serializerOptions, IErrorInfoProvider errorInfoProvider)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            // TODO: fix this: it modifies serializerOptions
            ConfigureOptions(errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings.
        /// Configuration defaults to no indenting and the specified instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="configureSerializerOptions">Specifies a callback used to configure the JSON serializer</param>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public GraphQLSerializer(Action<JsonSerializerOptions> configureSerializerOptions, IErrorInfoProvider errorInfoProvider)
        {
            if (configureSerializerOptions == null)
                throw new ArgumentNullException(nameof(configureSerializerOptions));

            if (errorInfoProvider == null)
                throw new ArgumentNullException(nameof(errorInfoProvider));

            _options = GetDefaultSerializerOptions(indent: false);
            configureSerializerOptions.Invoke(_options);

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

            if (!_options.Converters.Any(c => c.CanConvert(typeof(Inputs))))
            {
                _options.Converters.Add(new InputsConverter());
            }

            if (!_options.Converters.Any(c => c.CanConvert(typeof(GraphQLRequest))))
            {
                _options.Converters.Add(new GraphQLRequestConverter());
            }

            if (!_options.Converters.Any(c => c.CanConvert(typeof(List<GraphQLRequest>))))
            {
                _options.Converters.Add(new GraphQLRequestListConverter());
            }
        }

        private static JsonSerializerOptions GetDefaultSerializerOptions(bool indent)
            => new JsonSerializerOptions { WriteIndented = indent, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <inheritdoc/>
        public Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken)
            => JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken);

        /// <inheritdoc/>
        public ValueTask<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
            => JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken);

        /// <inheritdoc/>
        public string Write<T>(T value)
            => JsonSerializer.Serialize(value, _options);

        /// <inheritdoc/>
        public T Read<T>(string json)
            => JsonSerializer.Deserialize<T>(json, _options);

#if NET6_0_OR_GREATER
        /// <summary>
        /// Converts the <see cref="JsonDocument"/> representing a single JSON value into a <typeparamref name="T"/>.
        /// </summary>
        public T Read<T>(JsonDocument jsonDocument)
            => JsonSerializer.Deserialize<T>(jsonDocument, _options);

        /// <summary>
        /// Converts the <see cref="JsonElement"/> representing a single JSON value into a <typeparamref name="T"/>.
        /// </summary>
        public T ReadNode<T>(JsonElement jsonElement)
            => JsonSerializer.Deserialize<T>(jsonElement, _options);

        T IGraphQLSerializer.ReadNode<T>(object value)
            => ReadNode<T>((JsonElement)value);
#else
        T IGraphQLSerializer.ReadNode<T>(object value)
            => throw new NotSupportedException();
#endif
    }
}
