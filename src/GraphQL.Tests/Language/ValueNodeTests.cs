using System;
using System.Linq;
using GraphQL.Language;
using GraphQL.Language.AST;
using GraphQLParser;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Language
{
    public class ValueNodeTests
    {
        [Fact]
        public void double_cannot_contain_nan()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new FloatValue(double.NaN));
        }

        [Fact]
        public void double_cannot_contain_infinity()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new FloatValue(double.PositiveInfinity));
        }

        [Fact]
        public void double_cannot_contain_negativeinfinity()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new FloatValue(double.NegativeInfinity));
        }

        [Fact]
        public void string_cannot_be_null()
        {
            Should.Throw<ArgumentNullException>(() => new StringValue(null));
        }
    }
}
