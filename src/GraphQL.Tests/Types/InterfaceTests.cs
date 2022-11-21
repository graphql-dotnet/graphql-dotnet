using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class InterfaceTests : QueryTestBase<InterfaceTests.MySchema>
{
    [Fact]
    public void InterfaceWithNoObject()
    {
        AssertQuerySuccess("{ test { id } }", """
            {
                "test": {
                    "id": "123"
                }
            }
            """);
    }

    public class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<NonNullGraphType<MyInterfaceType>>("test", resolve: _ => new MyObject());
        }
    }

    public class MyInterfaceType : InterfaceGraphType<MyObject>
    {
        public MyInterfaceType()
        {
            Field("id", x => x.Id);
        }
    }

    public class MyObject
    {
        public string Id { get; set; } = "123";
    }
}
