namespace GraphQL.Types
{
    /// <summary>
    /// A type that has a name within the GraphQL schema.
    /// </summary>
    public interface INamedType
    {
        /// <summary>
        /// Gets or sets a type name within the GraphQL schema.
        /// Type names are case sensitive and consist of alphanumeric characters and underscores only. Type names cannot start with a digit.
        /// </summary>
        string Name { get; set; }
    }

    /// <summary>
    /// Represents a graph type within the GraphQL schema.
    /// </summary>
    public interface IGraphType : IProvideMetadata, INamedType
    {
        /// <summary>
        /// Gets or sets the description of the graph type.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets or sets the deprecation reason of the graph.
        /// </summary>
        string DeprecationReason { get; set; }

        string CollectTypes(TypeCollectionContext context);
    }
}
