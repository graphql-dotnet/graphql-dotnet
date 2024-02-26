using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class ListWithNullablesTest : QueryTestBase<ListWithNullablesSchema>
{
    [Fact]
    public void Can_Accept_Null_List_From_Literal()
    {
        const string query = """
            query _ {
                list {
                value
                }
            }
            """;
        const string expected = """
            {
                "list": [{ "value": "one"}, null, { "value": "three" }]
            }
            """;
        AssertQuerySuccess(query, expected);
    }
}

public class ListWithNullablesSchema : Schema
{
    public ListWithNullablesSchema()
    {
        Query = new ListWithNullablesQuery();
    }
}

public class ListWithNullablesQuery : ObjectGraphType
{
    public ListWithNullablesQuery()
    {
        Name = "Query";

        Field<ListGraphType<ListEntityGraphType>>("list")
            .Resolve(_ => new[] { new ListEntity { Value = "one" }, null, new ListEntity { Value = "three" } });
    }
}

public class ListEntity
{
    public string Value { get; set; }
}

public class ListEntityGraphType : ObjectGraphType<ListEntity>
{
    public ListEntityGraphType()
    {
        Name = "Entity";

        Field(x => x.Value);
    }
}
