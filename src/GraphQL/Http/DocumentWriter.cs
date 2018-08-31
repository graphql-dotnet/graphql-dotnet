using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Http
{
    public interface IDocumentWriter
    {
        Task WriteAsync(Stream stream, ExecutionResult value);

        [Obsolete("This method is obsolete and will be removed in the next major version.  Use WriteAsync instead.")]
        string Write(object value);
    }

    public class DocumentWriter : IDocumentWriter
    {
        private readonly JsonSerializer _serializer;
        private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

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

        public Task WriteAsync(Stream stream, ExecutionResult value)
        {
            using (var writer = new StreamWriter(stream, Utf8Encoding, 1024, true))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                _serializer.Serialize(jsonWriter, value);
            }

            return TaskExtensions.CompletedTask;
        }

        public string Write(object value)
        {
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
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static async Task<string> WriteToStringAsync(this IDocumentWriter writer,
            ExecutionResult value,
            Encoding encoding = null)
        {
            var resolvedEncoding = encoding ?? Encoding.UTF8;
            using (var stream = new MemoryStream())
            {
                await writer.WriteAsync(stream, value).ConfigureAwait(false);
#if NET45
                var length = (int) stream.Length;
                var offset = (int) stream.Seek(0, SeekOrigin.Begin);
                var buffer = stream.GetBuffer();

                return resolvedEncoding.GetString(buffer, offset, length - offset);
#else
// Will succeed since we use default MemoryStream constructor
                stream.TryGetBuffer(out var buffer);
                return resolvedEncoding.GetString(buffer.Array, buffer.Offset, buffer.Count);
#endif
            }
        }
    }
}
