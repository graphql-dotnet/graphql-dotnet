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

        [Theory]
        [InlineData(typeof(TestEnum))]
        [InlineData(typeof(FooType))]
        [InlineData(typeof(FooTypes))]
        [InlineData(typeof(Foo))]
        public void allow_non_relay_types_to_have_type_in_name(Type type)
        {
            Assert.Equal(type.Name, TypeExtensions.GraphQLName(type));
        }

        private enum TestEnum { }
        private enum FooType { }
        private enum FooTypes { }
        private class Foo { }
    }
}
