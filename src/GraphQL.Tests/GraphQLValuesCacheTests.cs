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

        Should.Throw<FormatException>(() => GraphQLValuesCache.GetInt("a")).Message.ShouldBe("Input string was not in a correct format.");
    }

    [Fact]
    public void GetLong()
    {
        for (long i = -15L; i <= 15L; ++i)
            GraphQLValuesCache.GetInt(i.ToString(CultureInfo.InvariantCulture)).ShouldBe(i);

        GraphQLValuesCache.GetLong("-1000000").ShouldBe(-1000000L);
        GraphQLValuesCache.GetLong("1000000").ShouldBe(1000000L);

        Should.Throw<FormatException>(() => GraphQLValuesCache.GetLong("a")).Message.ShouldBe("Input string was not in a correct format.");
    }
}
