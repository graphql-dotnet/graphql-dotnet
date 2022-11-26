using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/3415
public class Bug3415 : QueryTestBase<Bug3415.MySchema>
{
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

    [Fact]
    public void Union_SubFields_WithTypename()
    {
        string query = """
            query {
                test_with_typename {
                    __typename
                    ... on TypeA {
                        id
                    }
                }
            }
            """;

        string expected = """
            {
                "test_with_typename": {
                    "__typename": "TypeA",
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
                    context.SubFields.Count.ShouldBe(0); // we don't know concrete union member until we return it from this resolver on the last line
                    return new MyObject();
                });
            Field<MyUnionGraphType>("test_with_typename")
                .Resolve(context =>
                {
                    context.SubFields.Count.ShouldBe(1); // we don't know concrete union member until we return it from this resolver on the last line
                    context.SubFields.First().Key.ShouldBe("__typename");
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
