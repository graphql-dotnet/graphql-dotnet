using System.Text.Json;
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
        /// <summary>
        /// Returns the set of options used by the underlying serializer.
        /// </summary>
        protected JsonSerializerOptions SerializerOptions { get; }

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

            SerializerOptions = GetDefaultSerializerOptions(indent: false);
            configureSerializerOptions.Invoke(SerializerOptions);

            ConfigureOptions(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLSerializer"/> class with the specified settings
        /// and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="serializerOptions">Specifies the JSON serializer settings</param>
        public GraphQLSerializer(JsonSerializerOptions serializerOptions)
        {
            SerializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

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
            SerializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

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

            SerializerOptions = GetDefaultSerializerOptions(indent: false);
            configureSerializerOptions.Invoke(SerializerOptions);

            ConfigureOptions(errorInfoProvider);
        }

        private void ConfigureOptions(IErrorInfoProvider? errorInfoProvider)
        {
            if (!SerializerOptions.Converters.Any(c => c.CanConvert(typeof(ExecutionResult))))
            {
                SerializerOptions.Converters.Add(new ExecutionResultJsonConverter());
            }

            if (!SerializerOptions.Converters.Any(c => c.CanConvert(typeof(ExecutionError))))
            {
                SerializerOptions.Converters.Add(new ExecutionErrorJsonConverter(errorInfoProvider ?? new ErrorInfoProvider()));
            }

            if (!SerializerOptions.Converters.Any(c => c.CanConvert(typeof(ApolloTrace))))
            {
                SerializerOptions.Converters.Add(new ApolloTraceJsonConverter());
            }

            if (!SerializerOptions.Converters.Any(c => c.CanConvert(typeof(System.Numerics.BigInteger))))
            {
                SerializerOptions.Converters.Add(new JsonConverterBigInteger());
            }

            if (!SerializerOptions.Converters.Any(c => c.CanConvert(typeof(Inputs))))
            {
                SerializerOptions.Converters.Add(new InputsJsonConverter());
            }

            if (!SerializerOptions.Converters.Any(c => c.CanConvert(typeof(GraphQLRequest))))
            {
                SerializerOptions.Converters.Add(new GraphQLRequestJsonConverter());
            }

            if (!SerializerOptions.Converters.Any(c => c.CanConvert(typeof(List<GraphQLRequest>))))
            {
                SerializerOptions.Converters.Add(new GraphQLRequestListJsonConverter());
            }

            if (!SerializerOptions.Converters.Any(c => c.CanConvert(typeof(OperationMessage))))
            {
                SerializerOptions.Converters.Add(new OperationMessageJsonConverter());
            }
        }

        private static JsonSerializerOptions GetDefaultSerializerOptions(bool indent)
            => new JsonSerializerOptions { WriteIndented = indent };

        /// <inheritdoc/>
        public Task WriteAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default)
            => JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken);

#pragma warning disable CS8619 // Nullability of reference types doesn't match target type
        /// <inheritdoc/>
        public ValueTask<T?> ReadAsync<T>(Stream stream, CancellationToken cancellationToken = default)
            => JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
#pragma warning restore CS8619 // Nullability of reference types doesn't match target type

        /// <inheritdoc/>
        public string Serialize<T>(T? value)
            => JsonSerializer.Serialize(value, SerializerOptions);

        /// <inheritdoc/>
        public T? Deserialize<T>(string? json)
            => json == null ? default : JsonSerializer.Deserialize<T>(json, SerializerOptions);

        /*******
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
        ********/

        /// <summary>
        /// Converts the <see cref="JsonElement"/> representing a single JSON value into a <typeparamref name="T"/>.
        /// </summary>
        private T? ReadNode<T>(JsonElement jsonElement)
#if NET6_0_OR_GREATER
            => JsonSerializer.Deserialize<T>(jsonElement, SerializerOptions);
#else
            => JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), SerializerOptions);
#endif

        /// <summary>
        /// Converts the <see cref="JsonElement"/> representing a single JSON value into a <typeparamref name="T"/>.
        /// A <paramref name="value"/> of <see langword="null"/> returns <see langword="default"/>.
        /// Throws a <see cref="InvalidCastException"/> if <paramref name="value"/> is not a <see cref="JsonElement"/>.
        /// </summary>
        public T? ReadNode<T>(object? value)
            => value == null ? default : ReadNode<T>((JsonElement)value);
    }
}
