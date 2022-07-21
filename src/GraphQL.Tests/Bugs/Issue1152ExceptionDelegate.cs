using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue1152ExceptionDelegate : QueryTestBase<Issue1152Schema>
{
    [Fact]
    public void Issue1152_Should_Intercept_Unhandled_Exceptions()
    {
        var query = @"
query {
  somefield
}
";
        var expected = @"{
  ""somefield"": null
}";

        var list = new List<Exception>();

        var result = AssertQueryWithErrors(query, expected, expectedErrorCount: 1, unhandledExceptionDelegate: ctx =>
        {
            list.Add(ctx.OriginalException);
            ctx.Exception = new InvalidOperationException(ctx.OriginalException.Message);
            return Task.CompletedTask;
        });

        list.Count.ShouldBe(1);
        list[0].ShouldBeOfType<InvalidTimeZoneException>().StackTrace.ShouldNotBeNull();

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].InnerException.ShouldBeOfType<InvalidOperationException>().StackTrace.ShouldBeNull();
    }
}

public class Issue1152Schema : Schema
{
    public Issue1152Schema()
    {
        Query = new Issue1152Query();
    }
}

public class Issue1152Query : ObjectGraphType
{
    public Issue1152Query()
    {
        Field<StringGraphType>("somefield")
            .Resolve(_ => throw new InvalidTimeZoneException("Oops"));
    }
}
