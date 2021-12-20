using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL
{
    /// <summary>
    /// Deserializes a stream into an object hierarchy. Typically this would be deserializing a JSON stream into an instance of the GraphQLRequest class.
    /// </summary>
    public interface IGraphQLRequestReader
    {
        /// <summary>
        /// Asynchronously deserializes the specified stream to the specified object type.
        /// </summary>
        ValueTask<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken);
    }
}
