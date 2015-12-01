namespace GraphQL.Types
{
    public class EdgeType<TFrom, TTo> : ObjectGraphType
        where TTo : ObjectGraphType, new()
    {
        public EdgeType()
        {
            Name = string.Format("{0}{1}Edge",
                typeof(TFrom).GraphQLName(true), typeof(TTo).GraphQLName());
            Description = string.Format(
                "An edge in a connection between objects of types `{0}` and `{1}`.",
                typeof(TFrom).GraphQLName(), typeof(TTo).GraphQLName());

            Field<NonNullGraphType<StringGraphType>>("cursor", "A cursor for use in pagination",
                resolve: context => ((EdgeType<TFrom, TTo>)context.Source).Cursor);

            Field<TTo>("node", "The item at the end of the edge",
                resolve: context => ((EdgeType<TFrom, TTo>)context.Source).Node);
        }

        public string Cursor { get; set; }

        public TTo Node { get; set; }
    }
}
