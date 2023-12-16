using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class AbstractInputTests : QueryTestBase<AbstractInputSchema>
{
    [Fact]
    public void throws_literals()
    {
        Should.Throw<InvalidOperationException>(() => Schema.Initialize())
            .Message.ShouldBe("No public constructors found on CLR type 'MyInputClassBase'.");
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
