﻿using GraphQL.Types;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class TypeExtensionsTests
    {
        [Fact]
        public void supports_decimal_type()
        {
            typeof(decimal).GetGraphTypeFromType(true).ShouldBe(typeof(DecimalGraphType));
        }

        [Fact]
        public void supports_float_type()
        {
            typeof(float).GetGraphTypeFromType(true).ShouldBe(typeof(FloatGraphType));
        }

        [Fact]
        public void supports_list_type()
        {
            typeof(List<string>).GetGraphTypeFromType(true).ShouldBe(typeof(ListGraphType<StringGraphType>));
        }
    }
}
