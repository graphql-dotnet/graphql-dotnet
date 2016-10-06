namespace GraphQL.Types.Relay
{
    public class ConnectionType<TTo> : ObjectGraphType<object>
        where TTo : IGraphType
    {
        public ConnectionType()
        {
            Name = string.Format("{0}Connection", typeof(TTo).GraphQLName());
            Description = string.Format("A connection from an object to a list of objects of type `{0}`.",
                typeof(TTo).GraphQLName());

            Field<IntGraphType>()
                .Name("totalCount")
                .Description(
                    "A count of the total number of objects in this connection, ignoring pagination. " +
                    "This allows a client to fetch the first five objects by passing \"5\" as the argument " +
                    "to `first`, then fetch the total count so it could display \"5 of 83\", for example. " +
                    "In cases where we employ infinite scrolling or don't have an exact count of entries, " +
                    "this field will return `null`.");

            Field<NonNullGraphType<PageInfoType>>()
                .Name("pageInfo")
                .Description("Information to aid in pagination.");

            Field<ListGraphType<EdgeType<TTo>>>()
                .Name("edges")
                .Description("Information to aid in pagination.");

            Field<ListGraphType<TTo>>()
                .Name("items")
                .Description(
                    "A list of all of the objects returned in the connection. This is a convenience field provided " +
                    "for quickly exploring the API; rather than querying for \"{ edges { node } }\" when no edge data " +
                    "is needed, this field can be used instead. Note that when clients like Relay need to fetch " +
                    "the \"cursor\" field on the edge to enable efficient pagination, this shortcut cannot be used, " +
                    "and the full \"{ edges { node } } \" version should be used instead.");
        }
    }
}
