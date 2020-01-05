using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
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
            : this(new JsonSerializerSettings { Formatting = indent ? Formatting.Indented : Formatting.None })
        {
        }

        public DocumentWriter(JsonSerializerSettings settings)
        {
            _serializer = JsonSerializer.CreateDefault(settings);

            if (settings.ContractResolver == null)
            {
                _serializer.ContractResolver = new NewtonsoftContractResolver();
            }
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
    }
}
