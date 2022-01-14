using System;
using GraphQL.Language.AST;
using GraphQLParser.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Language
{
    public class ValueNodeTests
    {
        [Fact]
        public void floatvalue_cannot_contain_nan()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new FloatValue(double.NaN));
        }

        [Fact]
        public void stringvalue_cannot_contain_null()
        {
            Should.Throw<ArgumentNullException>(() => new StringValue(null));
        }

        [Fact]
        public void enumvalue_cannot_contain_null()
        {
            Should.Throw<ArgumentNullException>(() => new EnumValue((string)null));
            Should.Throw<ArgumentNullException>(() => new EnumValue((GraphQLName)null));
        }

        [Fact]
        public void enumvalue_cannot_contain_invalid_characters()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new EnumValue("kebab-enum"));
            Should.Throw<ArgumentOutOfRangeException>(() => new EnumValue(new GraphQLName("kebab-enum")));
        }
    }
}
