using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class TypeExtensionsTests
    {
        [Fact]
        public void supports_decimal_type()
        {
            typeof(decimal).GetGraphTypeFromType(true).ShouldBe(typeof(DecimalGraphType));
        }
    }
}
