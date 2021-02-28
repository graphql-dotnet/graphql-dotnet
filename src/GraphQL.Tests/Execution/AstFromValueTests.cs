using System;
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
            var result = ((IGraphType)null).ToAST(value);
            result.ShouldBeOfType<NullValue>();
        }

        [Fact]
        public void converts_string_to_string_value()
        {
            var result = new StringGraphType().ToAST("test");
            result.ShouldNotBeNull();
            result.ShouldBeOfType<StringValue>();
        }

        [Fact]
        public void converts_bool_to_boolean_value()
        {
            var result = new BooleanGraphType().ToAST(true);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<BooleanValue>();
        }

        [Fact]
        public void converts_long_to_long_value()
        {
            long val = 12345678910111213;
            var result = new LongGraphType().ToAST(val);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<LongValue>();
        }

        [Fact]
        public void converts_long_to_int_value()
        {
            long val = 12345678910111213;
            Should.Throw<OverflowException>(() => new IntGraphType().ToAST(val));
        }

        [Fact]
        public void converts_decimal_to_decimal_value()
        {
            decimal val = 1234.56789m;
            var result = new DecimalGraphType().ToAST(val);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<DecimalValue>();
        }

        [Fact]
        public void converts_int_to_int_value()
        {
            int val = 123;
            var result = new IntGraphType().ToAST(val);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<IntValue>();
        }

        [Fact]
        public void converts_double_to_float_value()
        {
            double val = 0.42;
            var result = new FloatGraphType().ToAST(val);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<FloatValue>();
        }

        [Fact]
        public void converts_byte_to_int_value()
        {
            byte value = 12;
            var result = new ByteGraphType().ToAST(value);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<IntValue>();
        }

        [Fact]
        public void converts_sbyte_to_int_value()
        {
            sbyte val = -12;
            var result = new SByteGraphType().ToAST(val);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<IntValue>();
        }

        [Fact]
        public void converts_uri_to_string_value()
        {
            var val = new Uri("http://www.wp.pl");
            var result = new UriGraphType().ToAST(val);
            result.ShouldNotBeNull();
            result.ShouldBeOfType<StringValue>();
        }
    }
}
