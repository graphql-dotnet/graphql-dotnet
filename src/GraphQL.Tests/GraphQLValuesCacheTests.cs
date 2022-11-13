using System.Globalization;

namespace GraphQL.Tests;

public class GraphQLValuesCacheTests
{
    [Fact]
    public void GetInt()
    {
        for (int i = -15; i <= 15; ++i)
            GraphQLValuesCache.GetInt(i.ToString(CultureInfo.InvariantCulture)).ShouldBe(i);

        GraphQLValuesCache.GetInt("-1000000").ShouldBe(-1000000);
        GraphQLValuesCache.GetInt("1000000").ShouldBe(1000000);

        Should.Throw<FormatException>(() => GraphQLValuesCache.GetInt("a")).Message.ShouldEndWith(" was not in a correct format.");
    }
}
