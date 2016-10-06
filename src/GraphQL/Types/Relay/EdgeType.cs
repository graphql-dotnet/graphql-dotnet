namespace GraphQL.Types.Relay
{
    public class EdgeType<TTo> : ObjectGraphType<object>
        where TTo : IGraphType
    {
        public EdgeType()
        {
            Name = string.Format("{0}Edge", typeof(TTo).GraphQLName());
            Description = string.Format(
                "An edge in a connection from an object to another object of type `{0}`.",
                typeof(TTo).GraphQLName());

            Field<NonNullGraphType<StringGraphType>>()
                .Name("cursor")
                .Description("A cursor for use in pagination");

            Field<TTo>()
                .Name("node")
                .Description("The item at the end of the edge");
        }
    }
}
