namespace GraphQL.Types.Relay.DataObjects
{
    /// <summary>
    /// Represents a connection result containing nodes and pagination information.
    /// </summary>
    /// <typeparam name="TNode">The data type.</typeparam>
    /// <typeparam name="TEdge">The edge type, typically <see cref="Edge{TNode}"/>.</typeparam>
    public class Connection<TNode, TEdge>
        where TEdge : Edge<TNode>
    {
        /// <summary>
        /// The total number of records available. Returns <see langword="null"/> if the total number is unknown.
        /// </summary>
        public int? TotalCount { get; set; }

        /// <summary>
        /// Additional pagination information for this result data set.
        /// </summary>
        public PageInfo? PageInfo { get; set; }

        /// <summary>
        /// The result data set, stored as a list of edges containing a node (the data) and a cursor (a unique identifier for the data).
        /// </summary>
        public List<TEdge>? Edges { get; set; }

        /// <summary>
        /// The result data set.
        /// </summary>
        public List<TNode?>? Items => Edges?.Select(edge => edge.Node).ToList();
    }

    /// <summary>
    /// Represents a connection result containing nodes and pagination information, with an
    /// edge type of <see cref="Edge{TNode}"/>.
    /// </summary>
    /// <typeparam name="TNode">The data type.</typeparam>
    public class Connection<TNode> : Connection<TNode, Edge<TNode>>
    {
    }
}
