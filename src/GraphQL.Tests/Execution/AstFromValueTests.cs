using GraphQL.Language;
using Should;

namespace GraphQL.Tests.Execution
{
    public class AstFromValueTests
    {
        readonly DocumentExecuter _executor = new DocumentExecuter();

        [Fact]
        public void converts_null_to_string_value()
        {
            var result = _executor.AstFromValue(null, null, null);
            result.ShouldNotBeNull();
            result.ShouldBeType<StringValue>();
        }

        [Fact]
        public void converts_string_to_string_value()
        {
            var result = _executor.AstFromValue(null, "test", null);
            result.ShouldNotBeNull();
            result.ShouldBeType<StringValue>();
        }

        [Fact]
        public void converts_bool_to_boolean_value()
        {
            var result = _executor.AstFromValue(null, true, null);
            result.ShouldNotBeNull();
            result.ShouldBeType<BooleanValue>();
        }

        [Fact]
        public void converts_long_to_long_value()
        {
            long val = 123;
            var result = _executor.AstFromValue(null, val, null);
            result.ShouldNotBeNull();
            result.ShouldBeType<LongValue>();
        }

        [Fact]
        public void converts_int_to_int_value()
        {
            int val = 123;
            var result = _executor.AstFromValue(null, val, null);
            result.ShouldNotBeNull();
            result.ShouldBeType<IntValue>();
        }

        [Fact]
        public void converts_double_to_float_value()
        {
            double val = 0.42;
            var result = _executor.AstFromValue(null, val, null);
            result.ShouldNotBeNull();
            result.ShouldBeType<FloatValue>();
        }
    }
}
