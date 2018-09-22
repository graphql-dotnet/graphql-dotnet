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

        Task<IByteResult> WriteAsync<T>(T value);

        [Obsolete("This method is obsolete and will be removed in the next major version.  Use WriteAsync instead.")]
        string Write(object value);
    }

    public class DocumentWriter : IDocumentWriter
    {
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        private readonly JsonArrayPool _jsonArrayPool = new JsonArrayPool(ArrayPool<char>.Shared);
        private readonly int _maxArrayLength = 1048576;
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

        public async Task WriteAsync<T>(Stream stream, T value)
        {
            using (var writer = new StreamWriter(stream, Utf8Encoding, 1024, true))
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

        public async Task<IByteResult> WriteAsync<T>(T value)
        {
            var pooledDocumentResult = new PooledByteResult(_pool, _maxArrayLength);
            var stream = pooledDocumentResult.Stream;
            try
            {
                await WriteAsync(stream, value).ConfigureAwait(false);
                pooledDocumentResult.InitResponseFromCurrentStreamPosition();
                return pooledDocumentResult;
            }
            catch (Exception)
            {
                pooledDocumentResult.Dispose();
                throw;
            }
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
            using (var buffer = await writer.WriteAsync(value).ConfigureAwait(false))
            {
                return buffer.Result.Array != null
                    ? resolvedEncoding.GetString(buffer.Result.Array, buffer.Result.Offset,
                        buffer.Result.Count)
                    : null;
            }
        }
    }
}
