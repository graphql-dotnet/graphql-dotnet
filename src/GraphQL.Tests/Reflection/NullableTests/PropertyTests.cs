using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Reflection;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Reflection.NullableTests
{
    public class PropertyTests
    {
        [Fact]
        public void NullThrows()
        {
            PropertyInfo property = null;
            Should.Throw<ArgumentNullException>(() => property.GetNullabilityInformation());
        }

        [Theory]
        [InlineData(typeof(NullableClass20), "Field1", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass20), "Field2", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass20), "Field3", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass20), "Field4", typeof(List<string>), Nullability.NonNullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass20), "Field5", typeof(int), Nullability.Nullable)]
        [InlineData(typeof(NullableClass20), "Field6", typeof(string), Nullability.NonNullable)]
        public void GetNullability(Type type, string propertyName, Type expectedType, Nullability expectedNullability, Type expectedType2 = null, Nullability? expectedNullability2 = null)
        {
            var property = type.GetProperty(propertyName);
            var actual = property.GetNullabilityInformation().ToList();
            actual.Count.ShouldBe(expectedType2 == null ? 1 : 2);
            actual[0].Type.ShouldBe(expectedType);
            actual[0].Nullable.ShouldBe(expectedNullability);
            if (expectedType2 != null)
            {
                actual[1].Type.ShouldBe(expectedType2);
                actual[1].Nullable.ShouldBe(expectedNullability2.Value);
            }
        }
    }
}
