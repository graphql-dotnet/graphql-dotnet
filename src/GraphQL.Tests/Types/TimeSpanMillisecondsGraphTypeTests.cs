using System;
using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class TimeSpanMillisecondsGraphTypeTests
    {
        private readonly TimeSpanMillisecondsGraphType _type = new TimeSpanMillisecondsGraphType();

        [Fact]
        public void serialize_string_returns_null()
        {
            CultureTestHelper.UseCultures(() =>
            {
                var actual = _type.Serialize("foo");
                actual.ShouldBeNull();
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
                var expected = (long)new TimeSpan(1, 2, 3, 4, 5).TotalMilliseconds;
                var actual = _type.Serialize(new TimeSpan(1, 2, 3, 4, 5));
                actual.ShouldBe(expected);
            });
        }

        [Fact]
        public void coerces_TimeSpan_to_timespan()
        {
            CultureTestHelper.UseCultures(() =>
            {
                var expected = new TimeSpan(1, 2, 3, 4, 5);

                var actual = _type.ParseValue(expected);

                actual.ShouldBe(expected);
            });
        }

        [Fact]
        public void coerces_int_to_timespan()
        {
            CultureTestHelper.UseCultures(() =>
            {
                var expected = new TimeSpan(1, 2, 3, 4, 5);
                var input = (int)new TimeSpan(1, 2, 3, 4, 5).TotalMilliseconds;

                var actual = _type.ParseValue(input);

                actual.ShouldBe(expected);
            });
        }

        [Fact]
        public void coerces_long_to_timespan()
        {
            CultureTestHelper.UseCultures(() => _type.ParseValue(12345678L).ShouldBe(new TimeSpan(0, 3, 25, 45, 678)));
        }
    }
}
