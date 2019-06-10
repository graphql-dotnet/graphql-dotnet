using System;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    public class TypeExtensionsTests
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        [InlineData(typeof(bool))]
        [InlineData(typeof(char))]
        [InlineData(typeof(TestEnum))]
        public void is_nullable_using_non_nullable_types(Type type)
        {
            TypeExtensions.IsNullable(type).ShouldBeFalse();
        }

        [Theory]
        [InlineData(typeof(int?))]
        [InlineData(typeof(double?))]
        [InlineData(typeof(float?))]
        [InlineData(typeof(bool?))]
        [InlineData(typeof(char?))]
        [InlineData(typeof(TestEnum?))]
        [InlineData(typeof(Nullable<>))]
        [InlineData(typeof(string))]
        public void is_nullable_using_nullable_types(Type type)
        {
            TypeExtensions.IsNullable(type).ShouldBeTrue();
        }

        private enum TestEnum { }
    }
}
