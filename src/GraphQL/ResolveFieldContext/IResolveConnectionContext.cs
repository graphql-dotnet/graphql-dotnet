using GraphQL.Resolvers;

namespace GraphQL.Builders
{
    /// <summary>
    /// Contains parameters pertaining to the currently executing <see cref="IFieldResolver"/>, along
    /// with helper properties for resolving forward and backward pagination requests on a
    /// connection type.
    /// </summary>
    public interface IResolveConnectionContext : IResolveFieldContext
    {
        /// <summary>
        /// Indicates if this connection only allows forward pagination requests.
        /// </summary>
        bool IsUnidirectional { get; }

        /// <summary>
        /// For a forward pagination request, returns the maximum number of edges to be returned.
        /// </summary>
        int? First { get; }

        /// <summary>
        /// For a backwards pagination request, returns the maximum number of edges to be returned.
        /// </summary>
        int? Last { get; }

        /// <summary>
        /// For a forward pagination request, returned edges should start immediately after the edge identified by this cursor.
        /// </summary>
        string? After { get; }

        /// <summary>
        /// For a backwards pagination request, returned edges should end immediately prior to the edge identified by this cursor.
        /// </summary>
        string? Before { get; }

        /// <summary>
        /// The maximum number of edges to be returned, or the specified default page size if <see cref="First"/> and
        /// <see cref="Last"/> are not specified.
        /// </summary>
        int? PageSize { get; }
    }

    /// <inheritdoc cref="IResolveConnectionContext"/>
    public interface IResolveConnectionContext<out T> : IResolveFieldContext<T>, IResolveConnectionContext
    {
    }
}
