namespace GraphQL.Types
{
    public class PageInfoType : ObjectGraphType
    {
        public PageInfoType()
        {
            Name = "PageInfo";
            Description = "Information about pagination in a connection.";

            Field<NonNullGraphType<BooleanGraphType>>("hasNextPage", "When paginating forwards, are there more items?");
            Field<NonNullGraphType<BooleanGraphType>>("hasPreviousPage", "When paginating backwards, are there more items?");
            Field<StringGraphType>("startCursor", "When paginating backwards, the cursor to continue.");
            Field<StringGraphType>("endCursor", "When paginating forwards, the cursor to continue.");
        }
    }

    public class PageInfo
    {
        public bool HasNextPage { get; set; }

        public bool HasPreviousPage { get; set; }

        public string StartCursor { get; set; }

        public string EndCursor { get; set; }
    }
}
