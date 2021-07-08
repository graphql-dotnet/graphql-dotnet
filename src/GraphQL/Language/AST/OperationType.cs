#nullable enable

namespace GraphQL.Language.AST
{
    /// <summary>
    /// An enumeration of the GraphQL operation types.
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// A query operation.
        /// </summary>
        Query,

        /// <summary>
        /// A mutation operation.
        /// </summary>
        Mutation,

        /// <summary>
        /// A subscription operation.
        /// </summary>
        Subscription
    }
}
