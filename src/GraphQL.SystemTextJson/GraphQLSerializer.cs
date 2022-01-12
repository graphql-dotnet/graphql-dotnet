using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Transport;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// Serializes an <see cref="ExecutionResult"/> (or any other object) to a stream using
    /// the <see cref="System.Text.Json"/> library.
    /// </summary>
    public class GraphQLSerializer : IGraphQLTextSerializer
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

            if (!_options.Converters.Any(c => c.CanConvert(typeof(ApolloTrace))))
            {
                _options.Converters.Add(new ApolloTraceJsonConverter());
            }

            if (!_options.Converters.Any(c => c.CanConvert(typeof(JsonConverterBigInteger))))
            {
                _options.Converters.Add(new JsonConverterBigInteger());
            }

            if (!_options.Converters.Any(c => c.CanConvert(typeof(Inputs))))
            {
                _options.Converters.Add(new InputsJsonConverter());
            }

            if (!_options.Converters.Any(c => c.CanConvert(typeof(GraphQLRequest))))
            {
                _options.Converters.Add(new GraphQLRequestJsonConverter());
            }

            if (!_options.Converters.Any(c => c.CanConvert(typeof(List<GraphQLRequest>))))
            {
                _options.Converters.Add(new GraphQLRequestListJsonConverter());
            }

            if (!_options.Converters.Any(c => c.CanConvert(typeof(OperationMessage))))
            {
                _options.Converters.Add(new OperationMessageJsonConverter());
            }
        }

        private static JsonSerializerOptions GetDefaultSerializerOptions(bool indent)
            => new JsonSerializerOptions { WriteIndented = indent };

        /// <inheritdoc/>
        public Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
            => JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken);

        /// <inheritdoc/>
        public ValueTask<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken = default)
            => JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken);

        /// <inheritdoc/>
        public string Serialize<T>(T value)
            => JsonSerializer.Serialize(value, _options);

        /// <inheritdoc/>
        public T Deserialize<T>(string json)
            => json == null ? default : JsonSerializer.Deserialize<T>(json, _options);

        /// <summary>
        /// Converts the <see cref="JsonDocument"/> representing a single JSON value into a <typeparamref name="T"/>.
        /// A <paramref name="jsonDocument"/> of <see langword="null"/> returns <see langword="default"/>.
        /// </summary>
        public T ReadDocument<T>(JsonDocument jsonDocument)
#if NET6_0_OR_GREATER
            => jsonDocument == null ? default : JsonSerializer.Deserialize<T>(jsonDocument, _options);
#else
            => jsonDocument == null ? default : JsonSerializer.Deserialize<T>(jsonDocument.RootElement.GetRawText(), _options);
#endif

        /// <summary>
        /// Converts the <see cref="JsonElement"/> representing a single JSON value into a <typeparamref name="T"/>.
        /// </summary>
        public T ReadNode<T>(JsonElement jsonElement)
#if NET6_0_OR_GREATER
            => JsonSerializer.Deserialize<T>(jsonElement, _options);
#else
            => JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), _options);
#endif

        /// <summary>
        /// Converts the <see cref="JsonElement"/> representing a single JSON value into a <typeparamref name="T"/>.
        /// A <paramref name="value"/> of <see langword="null"/> returns <see langword="default"/>.
        /// Throws a <see cref="InvalidCastException"/> if <paramref name="value"/> is not a <see cref="JsonElement"/>.
        /// </summary>
        public T ReadNode<T>(object value)
            => value == null ? default : ReadNode<T>((JsonElement)value);
    }
}
