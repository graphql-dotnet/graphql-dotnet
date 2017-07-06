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

        [Fact]
        public void supports_float_type()
        {
            typeof(float).GetGraphTypeFromType(true).ShouldBe(typeof(FloatGraphType));
        }

        [Fact]
        public void supports_array_of_float_type()
        {
            typeof(float[]).GetGraphTypeFromType(true).ShouldBe(typeof(ListGraphType<FloatGraphType>));
        }

        [Fact]
        public void support_list_of_float_type()
        {
            typeof(float[]).GetGraphTypeFromType(true).ShouldBe(typeof(ListGraphType<FloatGraphType>));
        }
    }
}
