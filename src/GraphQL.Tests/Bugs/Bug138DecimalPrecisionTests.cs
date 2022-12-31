using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug138DecimalPrecisionTests : QueryTestBase<DecimalSchema>
{
    [Fact]
    public async Task double_to_decimal_does_not_lose_precision()
    {
        var query = """
                mutation SetState{
                    set(request:24.15)
                }
            """;

        var result = await new DocumentExecuter().ExecuteAsync(new()
        {
            Schema = Schema,
            Query = query,
        }).ConfigureAwait(false);
        var expected = """{"data":{"set":24.15}}""";
        var stjJson = new SystemTextJson.GraphQLSerializer().Serialize(result);
        var nsjJson = new NewtonsoftJson.GraphQLSerializer().Serialize(result);
        stjJson.ShouldBe(expected);
        nsjJson.ShouldBe(expected);
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
