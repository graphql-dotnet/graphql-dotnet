namespace GraphQL.Types.Relay
{
    public class EdgeType<TNodeType> : ObjectGraphType<object>
        where TNodeType : IGraphType
    {
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
