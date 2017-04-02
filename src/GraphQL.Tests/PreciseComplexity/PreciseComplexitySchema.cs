namespace GraphQL.Tests.PreciseComplexity
{
    using GraphQL.Types;

    public class RootInterface : InterfaceGraphType
    {
        public RootInterface()
        {
            this.Name = "RootInterface";
            this.Field<StringGraphType>("string");
            this.Field<RootInterface>("this");
        }
    }

    public class NonRootInterface : InterfaceGraphType
    {
        public NonRootInterface()
        {
            this.Name = "NonRootInterface";
            this.Field<NonRootInterface>("this");
            this.Field<IntGraphType>("int");
        }
    }

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
                )).SetComplexity(
                (context, getArgument, childrenComplexity) =>
                    {
                        var limit = getArgument("limit") as int?;
                        return 1d + (limit ?? context.Configuration.DefaultCollectionChildrenCount) * childrenComplexity;
                    });
            this.Interface<RootInterface>();

            IsTypeOf = o => true;
        }
    }


    public class PreciseComplexitySchema : Schema
    {
        public PreciseComplexitySchema()
        {
            Query = new RootQuery();
            this.RegisterType<RootInterface>();
            this.RegisterType<NonRootInterface>();
        }
    }
}
