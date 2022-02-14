using GraphQL.DI;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Bugs
{
    public class BugWithCustomScalarsInDirective : QueryTestBase<BugWithCustomScalarsInDirectiveSchema>
    {
        public override void RegisterServices(IServiceRegister Services)
        {
            Services.Transient<BugWithCustomScalarsInDirectiveSchema>();
            Services.Transient<BugWithCustomScalarsInDirectiveQuery>();
        }

        [Fact]
        public void schema_should_be_initialized()
        {
            Schema.Initialize();
        }
    }

    public class BugWithCustomScalarsInDirectiveSchema : Schema
    {
        public BugWithCustomScalarsInDirectiveSchema(IServiceProvider provider, BugWithCustomScalarsInDirectiveQuery query)
            : base(provider)
        {
            Query = query;
            Directives.Register(new LinkDirective(), new SomeDirective());
        }
    }

    public class BugWithCustomScalarsInDirectiveQuery : ObjectGraphType
    {
        public BugWithCustomScalarsInDirectiveQuery()
        {
            Name = "Query";
            Field<StringGraphType>("str", resolve: _ => "aaa");
        }
    }

    public class LinkDirective : Directive
    {
        public LinkDirective() : base("link", DirectiveLocation.FieldDefinition, DirectiveLocation.Object, DirectiveLocation.Interface)
        {
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<UriGraphType>> { Name = "url" });
        }
    }

    public class SomeDirective : Directive
    {
        public SomeDirective() : base("some", DirectiveLocation.Scalar)
        {
            Arguments = new QueryArguments(new QueryArgument<GuidGraphType> { Name = "one" }, new QueryArgument<BigIntGraphType> { Name = "two" }, new QueryArgument<TimeSpanSecondsGraphType> { Name = "three" });
        }
    }
}
