namespace GraphQL.Tests.PreciseComplexity
{
    using GraphQL.Types;

    public class RootQuery : ObjectGraphType
    {
        public RootQuery()
        {
            Field<RootQuery>("this");
            this.Field<StringGraphType>("string");
            this.Field<ListGraphType<StringGraphType>>("stringList");
            this.Field<ListGraphType<RootQuery>>("thisList");
            this.Field<ListGraphType<RootQuery>>(
                "connection",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> { Name = "limit", DefaultValue = 0 }
                ));
        }
    }


    public class PreciseComplexitySchema : Schema
    {
        public PreciseComplexitySchema()
        {
            Query = new RootQuery();
        }
    }
}
