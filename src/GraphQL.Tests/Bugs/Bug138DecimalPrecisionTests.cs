using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug138DecimalPrecisionTests : QueryTestBase<DecimalSchema>
{
#if NETCOREAPP3_1
    [Fact]
#else
    [Fact(Skip = "24.149999999999999 with .NET Core < 3.1")]
#endif
    public void double_to_decimal_does_not_lose_precision()
    {
        var query = @"
                mutation SetState{
                    set(request:24.15)
                }
            ";

        var expected = @"{ ""set"": 24.15 }";

        AssertQuerySuccess(query, expected);
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
                var val = context.GetArgument<decimal>("request");
                val.ShouldBe(24.15m);
                return val;
            });
    }
}
