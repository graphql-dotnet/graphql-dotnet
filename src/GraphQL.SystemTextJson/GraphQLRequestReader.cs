using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.SystemTextJson
{
    public class GraphQLRequestReader : IGraphQLRequestReader
    {
        private readonly JsonSerializerOptions _options;

        public GraphQLRequestReader()
            : this(new JsonSerializerOptions())
        {
        }

        public GraphQLRequestReader(Action<JsonSerializerOptions> configureSerializerOptions)
        {
            if (configureSerializerOptions == null)
                throw new ArgumentNullException(nameof(configureSerializerOptions));

            _options = new JsonSerializerOptions();
            configureSerializerOptions.Invoke(_options);

            ConfigureOptions();
        }

        public GraphQLRequestReader(JsonSerializerOptions serializerOptions)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            // TODO: fix this: it modifies serializerOptions
            ConfigureOptions();
        }

        protected virtual void ConfigureOptions()
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

        public ValueTask<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
            => JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken);

        public T Read<T>(string json)
            => JsonSerializer.Deserialize<T>(json, _options);
    }
}
