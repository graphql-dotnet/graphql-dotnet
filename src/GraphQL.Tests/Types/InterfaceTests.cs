using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class InterfaceTests : QueryTestBase<InterfaceTests.MySchema>
{
    [Fact]
    public void InterfaceWithNoObject()
    {
        AssertQuerySuccess(
            """
            {
                noObjectTest {
                    id                    # as there is no object type defined for this
                                          # result, the interface resolver will be used
                                          # to return "123_interface"
                }
            }
            """,
            """
            {
                "noObjectTest": {
                    "id": "123_interface"
                }
            }
            """);
    }

    [Fact]
    public void InterfaceWithObject()
    {
        AssertQuerySuccess(
            """
            {
                objectTest {
                    ... on MyObject2 {
                        idOnMyObject2: id   # returns "234"
                    }
                    idOnInterface: id       # because MyObject2 implements the interface,
                                            # it will be resolved by MyObject2 and return
                                            # "234" instead of "234_interface"
                }
            }
            """,
            """
            {
                "objectTest": {
                    "idOnMyObject2": "234",
                    "idOnInterface": "234"
                }
            }
            """);
    }

    public class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
            RegisterType(new MyObject2GraphType());
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<NonNullGraphType<MyInterfaceType>>("noObjectTest", resolve: _ => new MyObject());
            Field<MyInterfaceType2>("objectTest", resolve: _ => new MyObject2());
        }
    }

    public class MyInterfaceType2 : InterfaceGraphType<MyObject2>
    {
        public MyInterfaceType2()
        {
            Field<StringGraphType>("id")
                .Resolve(ctx => ctx.Source.Id + "_interface");
        }
    }

    public class MyObject2GraphType : ObjectGraphType<MyObject2>
    {
        public MyObject2GraphType()
        {
            Name = "MyObject2";
            Field<StringGraphType>("id");
            Interface<MyInterfaceType2>();
        }
    }

    public class MyInterfaceType : InterfaceGraphType<MyObject>
    {
        public MyInterfaceType()
        {
            Field("id", x => x.Id)
                .Resolve(ctx => ctx.Source.Id + "_interface");
        }
    }

    public class MyObject
    {
        public string Id { get; set; } = "123";
    }

    public class MyObject2
    {
        public string Id { get; set; } = "234";
    }
}
