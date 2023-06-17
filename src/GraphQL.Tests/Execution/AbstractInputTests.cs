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
        var res = AssertQueryWithErrors(query, null, expectedErrorCount: 1, executed: false);
        res.Errors[0].Code.ShouldBe("INVALID_LITERAL");
        res.Errors[0].Message.ShouldBe("""Invalid literal for argument 'input' of field 'run'. Type 'GraphQL.Tests.Bugs.MyInputClassBase' is abstract and can not be used to construct objects from dictionary values. Please register a conversion within the ValueConverter or for input graph types override ParseDictionary method.""");
    }
}

public class AbstractInputSchema : Schema
{
    public AbstractInputSchema()
    {
        Query = new DummyType();
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
