using System;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class BugWithCustomScalarsInDirective : QueryTestBase<BugWithCustomScalarsInDirectiveSchema, MsDiContainer>
    {
        public BugWithCustomScalarsInDirective()
        {
            Services.Register<BugWithCustomScalarsInDirectiveSchema>();
            Services.Register<BugWithCustomScalarsInDirectiveQuery>();
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
            RegisterDirectives(new LinkDirective(), new SomeDirective());
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

    public class LinkDirective : DirectiveGraphType
    {
        public LinkDirective() : base("link", new[] { DirectiveLocation.FieldDefinition, DirectiveLocation.Object, DirectiveLocation.Interface })
        {
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<UriGraphType>> { Name = "url" });
        }
    }

    public class SomeDirective : DirectiveGraphType
    {
        public SomeDirective() : base("some", new[] { DirectiveLocation.Scalar })
        {
            Arguments = new QueryArguments(new QueryArgument<GuidGraphType> { Name = "one" }, new QueryArgument<BigIntGraphType> { Name = "two" }, new QueryArgument<TimeSpanSecondsGraphType> { Name = "three" });
        }
    }
}
