using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue1082 : QueryTestBase<Issue1082Schema>
{
    [Fact]
    public void Should_Return_DateTime_With_Time_Component()
    {
        var query = "{ dateTimeField }";
        AssertQuerySuccess(query, @"{""dateTimeField"": ""1000-10-10T01:02:03""}");
    }

    [Fact]
    public void Should_Return_Date_Without_Time_Component()
    {
        var query = "{ dateField }";
        AssertQuerySuccess(query, @"{""dateField"": ""1000-10-10""}");
    }
}

public class Issue1082Schema : Schema
{
    public Issue1082Schema()
    {
        var query = new ObjectGraphType<Model>();
        query.Field(f => f.DateTimeField).Resolve(_ => new DateTime(1000, 10, 10, 1, 2, 3));
        query.Field(f => f.DateField, type: typeof(DateGraphType)).Resolve(_ => new DateTime(1000, 10, 10, 1, 2, 3).Date);
        Query = query;
    }

    private class Model
    {
        public DateTime DateTimeField { get; set; }

        public DateTime DateField { get; set; }
    }
}
