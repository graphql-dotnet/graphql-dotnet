using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2387
    public class Issue2387 : QueryTestBase<Issue2387.MySchema>
    {
        [Fact]
        public void can_override_built_in_scalar_types()
        {
            AssertQuerySuccess("{testInt}", "{\"testInt\": 124}");
        }

        public class MySchema : Schema
        {
            public MySchema()
            {
                Query = new MyQuery();
                RegisterType(new MyIntGraphType());
            }
        }

        public class MyQuery : ObjectGraphType
        {
            public MyQuery()
            {
                Field<IntGraphType>("testInt", resolve: context => 123);
            }
        }

        public enum MyEnum
        {
            Hello
        }

        public class MyIntGraphType : IntGraphType
        {
            public MyIntGraphType()
            {
                Name = "Int";
            }

            public override object Serialize(object value) => value is int i ? (object)(i + 1) : null;
        }
    }

}
