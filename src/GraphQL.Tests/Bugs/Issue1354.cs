using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public sealed class Issue1354 : QueryTestBase<ValueTypeSchema>
{
    [Fact]
    public void can_resolve_property_on_value_type()
    {
        var query = "query { seconds }";
        var expected = @"{ ""seconds"": 42 }";
        AssertQuerySuccess(query, expected, root: TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void should_throw_on_unknown_property()
    {
        var query = "query { seconds1 }";
        var expected = @"{ ""seconds1"": null }";
        var result = AssertQueryWithErrors(query, expected, root: TimeSpan.FromSeconds(42), renderErrors: false, expectedErrorCount: 1);
        result.Errors[0].InnerException.ShouldBeOfType<InvalidOperationException>();
    }
}

public sealed class ValueTypeSchema : Schema
{
    public ValueTypeSchema()
    {
        var query = new ObjectGraphType<TimeSpan>();
        query.Field<IntGraphType>(name: "seconds");
        query.Field<IntGraphType>(name: "seconds1");
        Query = query;
    }
}
