namespace GraphQL.Types.Relay
{
    /// <summary>
    /// An edge graph type for the specified node graph type. The GraphQL type name
    /// defaults to {NodeType}Edge where {NodeType} is the GraphQL type name of
    /// the node graph type. This graph type assumes that the source (the result of
    /// the parent field's resolver) is <see cref="EdgeType{TNodeType}"/>
    /// or has the same members.
    /// </summary>
    /// <typeparam name="TNodeType">The graph type of the result data set's data type.</typeparam>
    public class EdgeType<TNodeType> : ObjectGraphType<object>
        where TNodeType : IGraphType
    {
        /// <inheritdoc/>
        public EdgeType()
        {
            Name = string.Format("{0}Edge", typeof(TNodeType).GraphQLName());
            Description = string.Format(
                "An edge in a connection from an object to another object of type `{0}`.",
                typeof(TNodeType).GraphQLName());

            Field<NonNullGraphType<StringGraphType>>()
                .Name("cursor")
                .Description("A cursor for use in pagination");

            Field<TNodeType>()
                .Name("node")
                .Description("The item at the end of the edge");
        }
    }
}
