using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Transports.Json;

namespace GraphQL
{
    /// <summary>
    /// Serializes and deserializes object hierarchies to/from a stream.
    /// Should include special support for <see cref="ExecutionResult"/> and transport-specific classes as necessary.
    /// Typical JSON-specific classes are providied within <see cref="Transports.Json">GraphQL.Transports.Json</see>.
    /// </summary>
    public interface IGraphQLTextSerializer : IGraphQLSerializer
    {
        /// <summary>
        /// Serializes the specified object to a string and returns it.
        /// Typically used to write <see cref="ExecutionResult"/> instances to a JSON result.
        /// </summary>
        string Write<T>(T value);

        /// <summary>
        /// Deserializes the specified string to the specified object type.
        /// Typically used to parse <see cref="GraphQLRequest"/> instances from JSON.
        /// </summary>
        T Read<T>(string value);
    }
}
