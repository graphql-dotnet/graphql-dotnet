using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL
{
    /// <summary>
    /// Serializes an object hiearchy to a stream. Typically this would be serializing the ExecutionResult class into a JSON stream.
    /// </summary>
    public interface IDocumentWriter
    {
        /// <summary>
        /// Asynchronously serializes the specified object to the specified stream.
        /// </summary>
        Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default);
    }
}
