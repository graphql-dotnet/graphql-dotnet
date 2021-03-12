using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// Serializes an <see cref="ExecutionResult"/> (or any other object) to a stream using
    /// the <see cref="System.Text.Json"/> library.
    /// </summary>
    public class DocumentWriter : IDocumentWriter
    {
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class with default settings:
        /// no indenting and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        public DocumentWriter()
            : this(indent: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class with the specified settings
        /// and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="indent">Indicates if child objects should be indented</param>
        public DocumentWriter(bool indent)
            : this(GetDefaultSerializerOptions(indent))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class with the specified settings.
        /// </summary>
        /// <param name="indent">Indicates if child objects should be indented</param>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public DocumentWriter(bool indent, IErrorInfoProvider errorInfoProvider)
            : this(GetDefaultSerializerOptions(indent), errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class with no indenting and the
        /// specified <see cref="IErrorInfoProvider"/>.
        /// </summary>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public DocumentWriter(IErrorInfoProvider errorInfoProvider)
            : this(false, errorInfoProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class configured with the specified callback.
        /// Configuration defaults to no indenting and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="configureSerializerOptions">Specifies a callback used to configure the JSON serializer</param>
        public DocumentWriter(Action<JsonSerializerOptions> configureSerializerOptions)
        {
            if (configureSerializerOptions == null)
                throw new ArgumentNullException(nameof(configureSerializerOptions));

            _options = GetDefaultSerializerOptions(indent: false);
            configureSerializerOptions.Invoke(_options);

            ConfigureOptions(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class with the specified settings
        /// and a default instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="serializerOptions">Specifies the JSON serializer settings</param>
        public DocumentWriter(JsonSerializerOptions serializerOptions)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            // TODO: fix this: it modifies serializerOptions
            ConfigureOptions(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class with the specified settings.
        /// </summary>
        /// <param name="serializerOptions">Specifies the JSON serializer settings</param>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public DocumentWriter(JsonSerializerOptions serializerOptions, IErrorInfoProvider errorInfoProvider)
        {
            _options = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

            // TODO: fix this: it modifies serializerOptions
            ConfigureOptions(errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class with the specified settings.
        /// Configuration defaults to no indenting and the specified instance of the <see cref="ErrorInfoProvider"/> class.
        /// </summary>
        /// <param name="configureSerializerOptions">Specifies a callback used to configure the JSON serializer</param>
        /// <param name="errorInfoProvider">Specifies the <see cref="IErrorInfoProvider"/> instance to use to serialize GraphQL errors</param>
        public DocumentWriter(Action<JsonSerializerOptions> configureSerializerOptions, IErrorInfoProvider errorInfoProvider)
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
        }

        private static JsonSerializerOptions GetDefaultSerializerOptions(bool indent)
            => new JsonSerializerOptions { WriteIndented = indent, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <inheritdoc/>
        public Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
            => JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken);
    }
}
