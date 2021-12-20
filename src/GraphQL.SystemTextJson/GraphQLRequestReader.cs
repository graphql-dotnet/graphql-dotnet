using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// Deserializes a <see cref="GraphQLRequest"/> (or any other object) from a stream using
    /// the <see cref="System.Text.Json"/> library.
    /// </summary>
    public class GraphQLRequestReader : IGraphQLRequestReader
    {
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLRequestReader"/> class with default settings.
        /// </summary>
        public GraphQLRequestReader()
            : this(new JsonSerializerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLRequestReader"/> class configured with the specified callback.
        /// </summary>
        public GraphQLRequestReader(Action<JsonSerializerOptions> configureSerializerOptions)
        {
            if (configureSerializerOptions == null)
                throw new ArgumentNullException(nameof(configureSerializerOptions));

            _options = new JsonSerializerOptions();
            configureSerializerOptions.Invoke(_options);

            ConfigureOptions();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphQLRequestReader"/> class configured with the specified options.
        /// </summary>
        public GraphQLRequestReader(JsonSerializerOptions serializerOptions)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            // TODO: fix this: it modifies serializerOptions
            ConfigureOptions();
        }

        private void ConfigureOptions()
        {
            if (!_options.Converters.Any(c => c.CanConvert(typeof(Inputs))))
            {
                _options.Converters.Add(new InputsConverter());
            }

            if (!_options.Converters.Any(c => c.CanConvert(typeof(JsonConverterBigInteger))))
            {
                _options.Converters.Add(new JsonConverterBigInteger());
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

        /// <inheritdoc/>
        public ValueTask<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
            => JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken);

        /// <summary>
        /// Deserializes the specified string to the specified object type.
        /// </summary>
        public T Read<T>(string json)
            => JsonSerializer.Deserialize<T>(json, _options);
    }
}
