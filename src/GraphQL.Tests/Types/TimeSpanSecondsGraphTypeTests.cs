using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class TimeSpanSecondsGraphTypeTests
    {
        private readonly TimeSpanSecondsGraphType _type = new TimeSpanSecondsGraphType();

        [Fact]
        public void serialize_string_throws()
        {
            CultureTestHelper.UseCultures(() =>
            {
                Should.Throw<InvalidOperationException>(() => _type.Serialize("foo"));
            });
        }

        [Fact]
        public void serialize_long()
        {
            CultureTestHelper.UseCultures(() =>
            {
                long input = 1;
                var actual = _type.Serialize(input);
                actual.ShouldBe(input);
            });
        }

        [Fact]
        public void serialize_int()
        {
            CultureTestHelper.UseCultures(() =>
            {
                int input = 1;
                var actual = _type.Serialize(input);
                actual.ShouldBe(input);
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
    }
}
