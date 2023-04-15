using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class InterfaceGraphTypeTests : QueryTestBase<InterfaceGraphTypeTests.MySchema>
{
    [Fact]
    public void VerifyArgumentsWork()
    {
        var query = """
            {
                test {
                    hello(name: "John Doe")
                }
            }
            """;
        var expected = """
            {
                "test": {
                    "hello": "John Doe"
                }
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void VerifyDefaultArgumentsWork()
    {
        var query = """
            {
                test {
                    hello
                }
            }
            """;
        var expected = """
            {
                "test": {
                    "hello": "world"
                }
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    public class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
            this.RegisterType<MyObject>();
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<MyInterface>("test")
                .Resolve(_ => 123);
        }
    }

    public class MyInterface : InterfaceGraphType
    {
        public MyInterface()
        {
            Field<string>("hello", true)
                .Argument<string>("name", true, arg => arg.DefaultValue = "world");
        }
    }

    public class MyObject : ObjectGraphType<int>
    {
        public MyObject()
        {
            Interface<MyInterface>();
            Field<string>("hello", true)
                .Argument<string>("name", true, arg => arg.DefaultValue = "world")
                .Resolve(ctx => ctx.GetArgument<string>("name"));
        }
    }
}
