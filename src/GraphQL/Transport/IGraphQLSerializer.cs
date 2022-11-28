using GraphQL.Transport;

namespace GraphQL
{
    /// <summary>
    /// Serializes and deserializes object hierarchies to/from a stream.
    /// Should include special support for <see cref="ExecutionResult"/>, <see cref="Inputs"/>
    /// and transport-specific classes as necessary.
    /// Typical classes needed by HTTP-based servers are provided within <see cref="Transport">GraphQL.Transport</see>.
    /// </summary>
    public interface IGraphQLSerializer
    {
        /// <summary>
        /// Asynchronously serializes the specified object to the specified stream.
        /// Typically used to write <see cref="ExecutionResult"/> instances to a JSON result.
        /// </summary>
        Task WriteAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deserializes the specified stream to the specified object type.
        /// Typically used to parse <see cref="GraphQLRequest"/> instances from JSON.
        /// </summary>
        ValueTask<T?> ReadAsync<T>(Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes the specified JSON element (element type depends on the serializer
        /// implementation) to the specified object type.
        /// A <paramref name="value"/> of <see langword="null"/> returns <see langword="default"/>.
        /// </summary>
        T? ReadNode<T>(object? value);

        /// <summary>
        /// Indicates whether this serializer makes asynchronous calls to the underlying stream
        /// while serializing or deserializing. This property is auxiliary API in nature and may
        /// help to avoid an additional/unnecessary buffering at caller side.
        /// </summary>
        bool IsNativelyAsync { get; }
    }
}
