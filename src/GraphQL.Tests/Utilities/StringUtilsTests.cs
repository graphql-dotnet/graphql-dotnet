using GraphQL.Utilities;
using Should;

namespace GraphQL.Tests.Utilities
{
    public class StringUtilsTests
    {
        [Fact]
        public void quoted_or_list_one()
        {
            StringUtils.QuotedOrList(new[] {"A"}).ShouldEqual("\"A\"");
        }

        [Fact]
        public void quoted_or_list_two()
        {
            StringUtils.QuotedOrList(new[] {"A", "B"}).ShouldEqual("\"A\" or \"B\"");
        }

        [Fact]
        public void quoted_or_list_three()
        {
            StringUtils.QuotedOrList(new[] {"A", "B", "C"}).ShouldEqual("\"A\", \"B\", or \"C\"");
        }
    }
}
