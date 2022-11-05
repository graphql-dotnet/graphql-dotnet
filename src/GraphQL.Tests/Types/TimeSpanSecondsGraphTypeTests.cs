using System.Collections;
using System.Numerics;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class TimeSpanSecondsGraphTypeTests
{
    public class TimeSpanSecondsGraphTypeTestsData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { (byte)1 };
            yield return new object[] { (sbyte)2 };
            yield return new object[] { (short)3 };
            yield return new object[] { (ushort)4 };
            yield return new object[] { (int)5 };
            yield return new object[] { (uint)6 };
            yield return new object[] { (long)7 };
            yield return new object[] { (ulong)8 };
            yield return new object[] { new BigInteger(9) };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private readonly TimeSpanSecondsGraphType _type = new();

    [Fact]
    public void parsevalue_throws()
    {
        CultureTestHelper.UseCultures(() => Should.Throw<InvalidOperationException>(() => _type.ParseValue("foo")));
    }

    [Fact]
    public void serialize_string_throws()
    {
        CultureTestHelper.UseCultures(() => Should.Throw<InvalidOperationException>(() => _type.Serialize("foo")));
    }

    [Theory]
    [ClassData(typeof(TimeSpanSecondsGraphTypeTestsData))]
    public void serialize_numerics(object value)
    {
        CultureTestHelper.UseCultures(() =>
        {
            var actual = _type.Serialize(value);
            actual.ShouldBeOfType<long>().ShouldBe(value is BigInteger b ? (long)b : Convert.ToInt64(value));
        });
    }

    [Fact]
    public void serialize_timespan_returns_total_seconds_as_long()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = (long)new TimeSpan(1, 2, 3, 4, 5).TotalSeconds;
            var actual = _type.Serialize(new TimeSpan(1, 2, 3, 4, 5));
            actual.ShouldBe(expected);
        });
    }

    [Theory]
    [InlineData((int)5)]
    [InlineData((long)7)]
    public void parseliteral_to_timespan(object value)
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = TimeSpan.FromSeconds(Convert.ToDouble(value));

            GraphQLValue ast = value switch
            {
                int i => new GraphQLIntValue(i),
                long l => new GraphQLIntValue(l),
                _ => null
            };
            var actual = _type.ParseLiteral(ast);

            actual.ShouldBe(expected);
        });
    }

    [Theory]
    [ClassData(typeof(TimeSpanSecondsGraphTypeTestsData))]
    public void parsevalue_to_timespan(object value)
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = TimeSpan.FromSeconds(value is BigInteger b ? (double)b : Convert.ToDouble(value));

            var actual = _type.ParseValue(value);

            actual.ShouldBe(expected);
        });
    }

    [Fact]
    public void coerces_int_to_timespan()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = new TimeSpan(1, 2, 3, 4);
            var input = (int)new TimeSpan(1, 2, 3, 4).TotalSeconds;

            var actual = _type.ParseValue(input);

            actual.ShouldBe(expected);
        });
    }

    [Fact]
    public void coerces_long_to_timespan()
    {
        CultureTestHelper.UseCultures(() => _type.ParseValue(123456789L).ShouldBe(new TimeSpan(1428, 21, 33, 9)));
    }

    [Fact]
    public void coerces_bigint_to_timespan()
    {
        CultureTestHelper.UseCultures(() => _type.ParseValue(new BigInteger(15)).ShouldBe(TimeSpan.FromSeconds(15)));
    }

    [Fact]
    public void coerces_timespan_to_timespan()
    {
        CultureTestHelper.UseCultures(() => _type.ParseValue(TimeSpan.FromSeconds(15)).ShouldBe(TimeSpan.FromSeconds(15)));
    }
}
