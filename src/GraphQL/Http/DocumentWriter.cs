using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GraphQL.Http
{
    public interface IDocumentWriter
    {
        Task WriteAsync<T>(Stream stream, T value);
    }   

    public class DocumentWriter : IDocumentWriter
    {
        private readonly JsonArrayPool _jsonArrayPool = new JsonArrayPool(ArrayPool<char>.Shared);
        private readonly JsonSerializer _serializer;
        internal static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

        public DocumentWriter()
            : this(indent: false)
        {
        }

        public DocumentWriter(bool indent)
            : this(
                indent ? Formatting.Indented : Formatting.None,
                new JsonSerializerSettings())
        {
        }

        public DocumentWriter(Formatting formatting, JsonSerializerSettings settings)
        {
            _serializer = JsonSerializer.CreateDefault(settings);
            _serializer.Formatting = formatting;
        }

        public async Task WriteAsync<T>(Stream stream, T value)
        {
            using (var writer = new HttpResponseStreamWriter(stream, Utf8Encoding))
            using (var jsonWriter = new JsonTextWriter(writer)
            {
                ArrayPool = _jsonArrayPool,
                CloseOutput = false,
                AutoCompleteOnClose = false
            })
            {
                _serializer.Serialize(jsonWriter, value);
                await jsonWriter.FlushAsync().ConfigureAwait(false);
            }
        }

        public string Write(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (!(value is ExecutionResult))
            {
                throw new ArgumentOutOfRangeException($"Expected {nameof(value)} to be a GraphQL.ExecutionResult, got {value.GetType().FullName}");
            }

            return this.WriteToStringAsync((ExecutionResult) value).GetAwaiter().GetResult();
        }
    }

    public static class DocumentWriterExtensions
    {
        /// <summary>
        /// Writes the <paramref name="value"/> to string.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static async Task<string> WriteToStringAsync(
            this IDocumentWriter writer,
            ExecutionResult value)
        {
            using(var stream = new MemoryStream())
            {
                await writer.WriteAsync(stream, value);
                stream.Position = 0;
                using (var reader = new StreamReader(stream, DocumentWriter.Utf8Encoding))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}
