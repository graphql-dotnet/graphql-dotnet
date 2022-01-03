using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Transports.Json;

namespace GraphQL
{
    /// <summary>
    /// Serializes and deserializes object hierarchies to/from a stream.
    /// Should include special support for <see cref="ExecutionResult"/>, <see cref="ErrorInfo"/>, <see cref="Inputs"/>
    /// and transport-specific classes as necessary.
    /// Typical JSON-specific classes are providied within <see cref="Transports.Json">GraphQL.Transports.Json</see>.
    /// </summary>
    public interface IGraphQLSerializer
    {
        /// <summary>
        /// Asynchronously serializes the specified object to the specified stream.
        /// Typically used to write <see cref="ExecutionResult"/> instances to a JSON result.
        /// </summary>
        Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously deserializes the specified stream to the specified object type.
        /// Typically used to parse <see cref="GraphQLRequest"/> instances from JSON.
        /// </summary>
        ValueTask<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken);

        /// <summary>
        /// Deserializes the specified JSON element to the specified object type.
        /// </summary>
        T ReadNode<T>(object value);
    }
}
