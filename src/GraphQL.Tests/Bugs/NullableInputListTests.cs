using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class NullableInputListTests : QueryTestBase<TestSchema>
{
    [Fact]
    public void Can_Accept_Null_List_From_Literal()
    {
        const string query = """
            query _ {
              example(testInputs:null)
            }
            """;
        const string expected = """
            {
              "example": "null"
            }
            """;
        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void Can_Accept_Null_List_From_Input()
    {
        const string query = """
            query _($inputs:[TestInput]) {
              example(testInputs: $inputs)
            }
            """;
        const string expected = """
            {
              "example": "null"
            }
            """;
        AssertQuerySuccess(query, expected, variables: new Inputs(new Dictionary<string, object?>
        {
            { "inputs", null }
        }));
    }
}

public class TestSchema : Schema
{
    public TestSchema()
    {
        Query = new TestQuery();
    }
}

public class TestQuery : ObjectGraphType
{
    public TestQuery()
    {
        Name = "Query";
        Field<StringGraphType>("example")
            .Argument<ListGraphType<TestInputType>>("testInputs")
            .Resolve(context =>
            {
                var testInputs = context.GetArgument<List<TestInput>>("testInputs");
                return testInputs == null
                    ? "null"
                    : "[" + string.Join(",", testInputs.Select(x => x == null ? "null" : x.Text)) + "]";
            }
        );
    }
}

public class TestInputType : InputObjectGraphType
{
    public TestInputType()
    {
        Name = "TestInput";
        Field<StringGraphType>("text");
    }
}

public class TestInput
{
    public string Text { get; set; }
}
