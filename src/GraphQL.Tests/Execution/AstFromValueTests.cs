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
            var result = value.AstFromValue(null, null);
            result.ShouldBeOfType<NullValue>();
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
            var result = val.AstFromValue(null, new LongGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<LongValue>();
        }

        [Fact]
        public void converts_long_to_int_value()
        {
            long val = 12345678910111213;
            Should.Throw<InvalidOperationException>(() => val.AstFromValue(null, new IntGraphType())).Message.ShouldBe("Unable to serialize '12345678910111213' to 'Int'.");
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

        [Fact]
        public void registers_ast_from_value_converters()
        {
            var schema = new Schema();
            schema.RegisterValueConverter(new ByteValueConverter());

            byte value = 12;
            var result = schema.FindValueConverter(value, null);
            result.ShouldNotBeNull("AST from value converter should be registered");
            result.ShouldBeOfType<ByteValueConverter>();
        }

        [Fact]
        public void converts_byte_to_byte_value()
        {
            var schema = new Schema();

            byte value = 12;
            var result = value.AstFromValue(schema, new ByteGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<ByteValue>();
        }

        [Fact]
        public void converts_sbyte_to_sbyte_value()
        {
            sbyte val = -12;
            var result = val.AstFromValue(null, new SByteGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<SByteValue>();
        }

        [Fact]
        public void converts_uri_to_uri_value()
        {
            var val = new Uri("http://www.wp.pl");
            var result = val.AstFromValue(null, new UriGraphType());
            result.ShouldNotBeNull();
            result.ShouldBeOfType<UriValue>();
        }
    }

    internal class ByteValueConverter : IAstFromValueConverter
    {
        public bool Matches(object value, IGraphType type)
        {
            return value is byte;
        }

        public IValue Convert(object value, IGraphType type)
        {
            return new ByteValue((byte)value);
        }
    }
}
