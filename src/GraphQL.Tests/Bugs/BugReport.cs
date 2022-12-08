using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug3444
{
    [Fact]
    public void Rethrow_Schema_Initialization_Failure()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<TestOneType>("test1");
        queryType.Field<TestTwoType>("test2");
        var schema = new Schema() { Query = queryType };
        var msg1 = Should.Throw<InvalidOperationException>(() => schema.Initialize()).Message;
        var msg2 = Should.Throw<InvalidOperationException>(() => schema.Initialize()).Message;
        msg1.ShouldBe(msg2);
    }

    private class TestOneType : EnumerationGraphType<TestEnum>
    {
    }

    private class TestTwoType : EnumerationGraphType<TestEnum>
    {
    }

    private enum TestEnum
    {
        One,
        Two,
        Three
    }
}
