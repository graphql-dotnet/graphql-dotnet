using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug138DecimalPrecisionTests : QueryTestBase<DecimalSchema>
    {
        [Fact]
        public void double_to_decimal_does_not_lose_precision()
        {
            var query = @"
                mutation SetState{
                    set(request:24.15)
                }
            ";

            var expected = @"{
              set: 24.15
            }";

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
            Field<DecimalGraphType>(
                "set",
                arguments: new QueryArguments(new QueryArgument<DecimalGraphType> { Name = "request"}),
                resolve: context =>
                {
                    var val = context.GetArgument<decimal>("request");
                    return val;
                });
        }
    }
}
