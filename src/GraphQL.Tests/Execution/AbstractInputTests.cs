using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class AbstractInputTests : QueryTestBase<AbstractInputSchema>
{
    [Fact]
    public void throws_literals()
    {
        const string query = """
            mutation M {
              run(input: { id: "123" })
            }
            """;
        const string expected = """{ "run": null }""";
        var res = AssertQueryWithErrors(query, expected, expectedErrorCount: 1);
        res.Errors![0].Code.ShouldBe("INVALID_OPERATION");
    }
}

public class AbstractInputSchema : Schema
{
    public AbstractInputSchema()
    {
        Mutation = new AbstractInputMutation();
    }
}

public class AbstractInputMutation : ObjectGraphType
{
    public AbstractInputMutation()
    {
        Field<StringGraphType>("run")
            .Argument<MyInputGraphType>("input")
            .Resolve(ctx => ctx.GetArgument<int>("input")); // type does not matter here
    }
}

public abstract class MyInputClassBase
{
    public string Id { get; set; }
}

public class MyInputGraphType : InputObjectGraphType<MyInputClassBase>
{
    public MyInputGraphType()
    {
        Field<NonNullGraphType<StringGraphType>>("id");
    }
}
