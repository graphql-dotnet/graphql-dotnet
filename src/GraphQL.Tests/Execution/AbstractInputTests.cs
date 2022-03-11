using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class AbstractInputTests : QueryTestBase<AbstractInputSchema>
{
    [Fact]
    public void throws_literals()
    {
        var query = @"
mutation M {
  run(input: { id: ""123"" })
}
";
        var expected = @"{ ""run"": null }";
        var res = AssertQueryWithErrors(query, expected, expectedErrorCount: 1);
        res.Errors[0].Code.ShouldBe("INVALID_OPERATION");
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
        Field<StringGraphType>(
            "run",
            arguments: new QueryArguments(new QueryArgument<MyInputGraphType> { Name = "input" }),
            resolve: ctx => ctx.GetArgument<int>("input")); // type does not matter here
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
