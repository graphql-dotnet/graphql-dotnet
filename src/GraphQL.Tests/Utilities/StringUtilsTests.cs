using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities;

public class StringUtilsTests
{
    [Fact]
    public void quoted_or_list_one()
    {
        StringUtils.QuotedOrList(["A"]).ShouldBe("'A'");
    }

    [Fact]
    public void quoted_or_list_two()
    {
        StringUtils.QuotedOrList(["A", "B"]).ShouldBe("'A' or 'B'");
    }

    [Fact]
    public void quoted_or_list_three()
    {
        StringUtils.QuotedOrList(["A", "B", "C"]).ShouldBe("'A', 'B', or 'C'");
    }
}
