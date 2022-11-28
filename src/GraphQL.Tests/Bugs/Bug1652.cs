namespace GraphQL.Tests.Bugs;

public class Bug1652
{
    [Fact]
    public void ExecutionError_Should_Accept_Object_Keys()
    {
        var ex = new ExecutionError("an error");
        ex.Data.Add(new object(), "details");
        ex.Data.Count.ShouldBe(1);
    }
}
