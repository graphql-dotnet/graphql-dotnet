using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities;

public class StringUtilsTests
{
    [Fact]
    public void quoted_or_list_one()
    {
        StringUtils.QuotedOrList(new[] { "A" }).ShouldBe("'A'");
    }

    [Fact]
    public void quoted_or_list_two()
    {
        StringUtils.QuotedOrList(new[] { "A", "B" }).ShouldBe("'A' or 'B'");
    }

    [Fact]
    public void quoted_or_list_three()
    {
        StringUtils.QuotedOrList(new[] { "A", "B", "C" }).ShouldBe("'A', 'B', or 'C'");
    }
}
