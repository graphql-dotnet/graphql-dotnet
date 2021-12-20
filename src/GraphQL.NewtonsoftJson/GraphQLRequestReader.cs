using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// Deserializes a <see cref="GraphQLRequest"/> (or any other object) from a stream using
    /// the <see cref="Newtonsoft.Json"/> library.
    /// </summary>
    public class GraphQLRequestReader : IGraphQLRequestReader
    {
        private readonly JsonSerializerSettings _options;
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLRequestReader"/> class with default settings.
        /// </summary>
        public GraphQLRequestReader()
            : this(new JsonSerializerSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLRequestReader"/> class configured with the specified callback.
        /// </summary>
        public GraphQLRequestReader(Action<JsonSerializerSettings> configureSerializerOptions)
        {
            if (configureSerializerOptions == null)
                throw new ArgumentNullException(nameof(configureSerializerOptions));

            _options = new JsonSerializerSettings();
            configureSerializerOptions.Invoke(_options);

            _serializer = BuildSerializer();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLRequestReader"/> class configured with the specified options.
        /// </summary>
        public GraphQLRequestReader(JsonSerializerSettings serializerOptions)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            // TODO: fix this: it modifies serializerOptions
            _serializer = BuildSerializer();
        }

        private JsonSerializer BuildSerializer()
        {
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

            return JsonSerializer.CreateDefault(_options);
        }

        /// <inheritdoc/>
        public ValueTask<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
        {
            //note: do not dispose of stringReader or else the underlying stream will be disposed
            var stringReader = new StreamReader(stream, System.Text.Encoding.UTF8);
            using var jsonReader = new JsonTextReader(stringReader);
            return new ValueTask<T>(_serializer.Deserialize<T>(jsonReader));
        }

        /// <summary>
        /// Deserializes the specified string to the specified object type.
        /// </summary>
        public T Read<T>(StringReader json)
        {
            using var jsonReader = new JsonTextReader(json);
            return _serializer.Deserialize<T>(jsonReader);
        }

        /// <inheritdoc cref="Read{T}(StringReader)"/>
        public T Read<T>(string json) => Read<T>(new StringReader(json));
    }
}
