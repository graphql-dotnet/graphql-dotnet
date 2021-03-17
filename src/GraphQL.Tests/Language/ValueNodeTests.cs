using System;
using GraphQL.Language.AST;
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
        public void floatvalue_cannot_contain_infinity()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new FloatValue(double.PositiveInfinity));
        }

        [Fact]
        public void floatvalue_cannot_contain_negativeinfinity()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new FloatValue(double.NegativeInfinity));
        }

        [Fact]
        public void stringvalue_cannot_be_null()
        {
            Should.Throw<ArgumentNullException>(() => new StringValue(null));
        }
    }
}
