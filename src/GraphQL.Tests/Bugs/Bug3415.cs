using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug3415 : QueryTestBase<Bug3415.MySchema>
{
    [Fact]
    public void Union_SubFields_WithTypename()
    {
        string query = """
            query {
                test {
                    __typename
                    ... on TypeA {
                        id
                    }
                }
            }
            """;

        string expected = """
            {
                "test": {
                    "__typename": "TypeA",
                    "id": "123"
                }
            }
            """;
        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void Union_SubFields_WithoutTypename()
    {
        string query = """
            query {
                test {
                    ... on TypeA {
                        id
                    }
                }
            }
            """;

        string expected = """
            {
                "test": {
                    "id": "123"
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
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<MyUnionGraphType>("test")
                .Resolve(context =>
                {
                    _ = context.SubFields;
                    return new MyObject();
                });

            Field<MyUnionGraphType>("test2")
                .Resolve(context =>
                {
                    _ = context.SubFields;
                    return new MyObject();
                });
        }
    }

    public class MyUnionGraphType : UnionGraphType
    {
        public MyUnionGraphType()
        {
            Type<AutoRegisteringObjectGraphType<MyObject>>();
        }
    }

    [Name("TypeA")]
    public class MyObject
    {
        public string Id { get; set; } = "123";
    }
}
