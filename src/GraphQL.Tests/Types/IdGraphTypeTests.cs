using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class IdGraphTypeTests
    {
        private readonly IdGraphType _type = new IdGraphType();

        [Fact]
        public void parse_value_null_to_null()
        {
            _type.ParseValue(null).ShouldBe(null);
        }

        [Fact]
        public void parse_value_string_to_identifier()
        {
            _type.ParseValue("abcd").ShouldBe("abcd");
        }

        [Fact]
        public void parse_value_quoted_string_to_identifier()
        {
            _type.ParseValue("\"12345\"").ShouldBe("12345");
        }

        [Fact]
        public void parse_literal_string_value_quoted_string_to_identifier()
        {
            _type.ParseLiteral(new StringValue("\"12345\"")).ShouldBe("12345");
        }

        [Fact]
        public void parse_literal_int_value_to_identifier()
        {
            int val = 12345;
            _type.ParseLiteral(new IntValue(12345)).ShouldBe(val);
        }

        [Fact]
        public void parse_literal_long_value_to_identifier()
        {
            long val = 12345;
            _type.ParseLiteral(new LongValue(12345)).ShouldBe(val);
        }
    }
}
