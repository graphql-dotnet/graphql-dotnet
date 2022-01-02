using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for document writers.
    /// </summary>
    public static class GraphQLSerializerExtensions
    {
        private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);

        /// <summary>
        /// Writes the <paramref name="value"/> to string.
        /// </summary>
        [Obsolete] //provided only to service tests
        public static async ValueTask<string> WriteToStringAsync<T>(this IGraphQLSerializer serializer, T value, CancellationToken cancellationToken = default)
        {
            if (serializer is IGraphQLTextSerializer textSerializer)
                return textSerializer.Write(value);

            using var stream = new MemoryStream();
            await serializer.WriteAsync(stream, value, cancellationToken).ConfigureAwait(false);
            stream.Position = 0;
            using var reader = new StreamReader(stream, _utf8Encoding);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}
