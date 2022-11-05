using GraphQL.Transport;

namespace GraphQL
{
    /// <summary>
    /// Serializes and deserializes object hierarchies to/from a string as well as to/from a stream.
    /// Should include special support for <see cref="ExecutionResult"/>, <see cref="Inputs"/>
    /// and transport-specific classes as necessary.
    /// Typical classes needed by HTTP-based servers are provided within <see cref="Transport">GraphQL.Transport</see>.
    /// </summary>
    public interface IGraphQLTextSerializer : IGraphQLSerializer
    {
        /// <summary>
        /// Serializes the specified object to a string and returns it.
        /// Typically used to write <see cref="ExecutionResult"/> instances to a JSON result.
        /// </summary>
        string Serialize<T>(T? value);

        /// <summary>
        /// Deserializes the specified string to the specified object type.
        /// Typically used to parse <see cref="GraphQLRequest"/> instances from JSON.
        /// A <paramref name="value"/> of <see langword="null"/> returns <see langword="default"/>.
        /// </summary>
        T? Deserialize<T>(string? value);
    }
}
