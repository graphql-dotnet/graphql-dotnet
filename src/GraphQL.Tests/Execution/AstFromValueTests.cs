using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class AstFromValueTests
    {
        [Fact]
        public void converts_null_to_null()
        {
            object value = null;
            var result = value.AstFromValue(null, null);
            result.ShouldBeNull();
        }

        [Fact]
        public void converts_string_to_string_value()
        {
            var result = "test".AstFromValue(null, new StringGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<StringValue>();
        }

        [Fact]
        public void converts_bool_to_boolean_value()
        {
            var result = true.AstFromValue(null, new BooleanGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<BooleanValue>();
        }

        [Fact]
        public void converts_long_to_long_value()
        {
            long val = 12345678910111213;
            var result = val.AstFromValue(null, new IntGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<LongValue>();
        }

        [Fact]
        public void converts_decimal_to_decimal_value()
        {
            decimal val = 1234.56789m;
            var result = val.AstFromValue(null, new DecimalGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<DecimalValue>();
        }

        [Fact]
        public void converts_int_to_int_value()
        {
            int val = 123;
            var result = val.AstFromValue(null, new IntGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<IntValue>();
        }

        [Fact]
        public void converts_double_to_float_value()
        {
            double val = 0.42;
            var result = val.AstFromValue(null, new FloatGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<FloatValue>();
        }
    }
}
