using System;
using System.Collections.Generic;
using System.Reflection;
using GraphQL.Types;
using Moq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    public class GraphQLValuesCacheTests
    {
        [Fact]
        public void GetInt()
        {
            GraphQLValuesCache.GetInt("-1000000").ShouldBe(-1000000);
            GraphQLValuesCache.GetInt("-10").ShouldBe(-10);
            GraphQLValuesCache.GetInt("-9").ShouldBe(-9);
            GraphQLValuesCache.GetInt("-5").ShouldBe(-5);
            GraphQLValuesCache.GetInt("0").ShouldBe(0);
            GraphQLValuesCache.GetInt("5").ShouldBe(5);
            GraphQLValuesCache.GetInt("9").ShouldBe(9);
            GraphQLValuesCache.GetInt("10").ShouldBe(10);
            GraphQLValuesCache.GetInt("1000000").ShouldBe(1000000);

            Should.Throw<FormatException>(() => GraphQLValuesCache.GetInt("a")).Message.ShouldBe("Input string was not in a correct format.");
        }

        [Fact]
        public void GetLong()
        {
            GraphQLValuesCache.GetLong("-1000000").ShouldBe(-1000000L);
            GraphQLValuesCache.GetLong("-10").ShouldBe(-10L);
            GraphQLValuesCache.GetLong("-9").ShouldBe(-9L);
            GraphQLValuesCache.GetLong("-5").ShouldBe(-5L);
            GraphQLValuesCache.GetLong("0").ShouldBe(0L);
            GraphQLValuesCache.GetLong("5").ShouldBe(5L);
            GraphQLValuesCache.GetLong("9").ShouldBe(9L);
            GraphQLValuesCache.GetLong("10").ShouldBe(10L);
            GraphQLValuesCache.GetLong("1000000").ShouldBe(1000000L);

            Should.Throw<FormatException>(() => GraphQLValuesCache.GetLong("a")).Message.ShouldBe("Input string was not in a correct format.");
        }
    }
}
