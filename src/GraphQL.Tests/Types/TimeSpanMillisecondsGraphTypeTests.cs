using System.Collections;
using System.Numerics;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class TimeSpanMillisecondsGraphTypeTests
{
    public class TimeSpanMillisecondsGraphTypeTestsData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [(byte)1];
            yield return [(sbyte)2];
            yield return [(short)3];
            yield return [(ushort)4];
            yield return [(int)5];
            yield return [(uint)6];
            yield return [(long)7];
            yield return [(ulong)8];
            yield return [new BigInteger(9)];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private readonly TimeSpanMillisecondsGraphType _type = new();

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

    [TheoryEx]
    [ClassData(typeof(TimeSpanMillisecondsGraphTypeTestsData))]
    public void serialize_numerics(object value)
    {
        CultureTestHelper.UseCultures(() =>
        {
            object? actual = _type.Serialize(value);
            actual.ShouldBeOfType<long>().ShouldBe(value is BigInteger b ? (long)b : Convert.ToInt64(value));
        });
    }

    [Fact]
    public void serialize_timespan_returns_total_seconds_as_long()
    {
        CultureTestHelper.UseCultures(() =>
        {
            long? expected = (long)new TimeSpan(1, 2, 3, 4, 5).TotalMilliseconds;
            object? actual = _type.Serialize(new TimeSpan(1, 2, 3, 4, 5));
            actual.ShouldBe(expected);
        });
    }

    [Fact]
    public void coerces_TimeSpan_to_timespan()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = new TimeSpan(1, 2, 3, 4, 5);

            object? actual = _type.ParseValue(expected);

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
            var expected = TimeSpan.FromMilliseconds(Convert.ToDouble(value));

            GraphQLValue? ast = value switch
            {
                int i => new GraphQLIntValue(i),
                long l => new GraphQLIntValue(l),
                _ => null
            };
            object? actual = _type.ParseLiteral(ast!);

            actual.ShouldBe(expected);
        });
    }

    [TheoryEx]
    [ClassData(typeof(TimeSpanMillisecondsGraphTypeTestsData))]
    public void parsevalue_to_timespan(object value)
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = TimeSpan.FromMilliseconds(value is BigInteger b ? (double)b : Convert.ToDouble(value));

            object? actual = _type.ParseValue(value);

            actual.ShouldBe(expected);
        });
    }

    [Fact]
    public void coerces_int_to_timespan()
    {
        CultureTestHelper.UseCultures(() =>
        {
            var expected = new TimeSpan(1, 2, 3, 4);
            int? input = (int)new TimeSpan(1, 2, 3, 4).TotalMilliseconds;

            object? actual = _type.ParseValue(input);

            actual.ShouldBe(expected);
        });
    }

    [Fact]
    public void coerces_long_to_timespan()
    {
        CultureTestHelper.UseCultures(() => _type.ParseValue(12345678L).ShouldBe(new TimeSpan(0, 3, 25, 45, 678)));
    }

    [Fact]
    public void coerces_bigint_to_timespan()
    {
        CultureTestHelper.UseCultures(() => _type.ParseValue(new BigInteger(15)).ShouldBe(TimeSpan.FromMilliseconds(15)));
    }

    [Fact]
    public void coerces_timespan_to_timespan()
    {
        CultureTestHelper.UseCultures(() => _type.ParseValue(TimeSpan.FromSeconds(15)).ShouldBe(TimeSpan.FromSeconds(15)));
    }
}
