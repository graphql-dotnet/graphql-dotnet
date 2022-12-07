using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug3444
{
    [Fact]
    public void Test_Rethrow_Schema_Initialization_Failure()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<TestOneType>("test1");
        queryType.Field<TestTwoType>("test2");
        var schema = new Schema() { Query = queryType };
        var msg1 = Should.Throw<InvalidOperationException>(() => schema.Initialize()).ToString();
        var msg2 = Should.Throw<InvalidOperationException>(() => schema.Initialize()).ToString();
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
