using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug138DecimalPrecisionTests : QueryTestBase<DecimalSchema>
{
    [Fact]
    public void double_to_decimal_does_not_lose_precision()
    {
        const string query = """
                mutation SetState{
                    set(request:24.15)
                }
            """;

        const string expected = """
            {
              "data": {
                "set": 24.15
              }
            }
            """;

        AssertQuerySuccess(query, expected, suppressSerializeExpected: true);
    }
}

public class DecimalSchema : Schema
{
    public DecimalSchema()
    {
        Mutation = new DecimalMutation();
    }
}

public class DecimalMutation : ObjectGraphType
{
    public DecimalMutation()
    {
        Field<DecimalGraphType>("set")
            .Argument<DecimalGraphType>("request")
            .Resolve(context =>
            {
                decimal val = context.GetArgument<decimal>("request");
                val.ShouldBe(24.15m);
                return val;
            });
    }
}
