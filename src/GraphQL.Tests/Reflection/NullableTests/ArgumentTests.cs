using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Reflection;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Reflection.NullableTests
{
    public class ArgumentTests
    {
        [Theory]
        [InlineData(typeof(NullableClass9), "Field1", "arg1", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass9), "Field1", "arg2", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass10), "Field1", "arg1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass10), "Field1", "arg2", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass11), "Field2", "arg1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass11), "Field2", "arg2", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass13), "Field2", "arg1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass13), "Field2", "arg2", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass13), "Field2", "arg3", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass13), "Field2", "arg4", typeof(int), Nullability.Nullable)]
        [InlineData(typeof(NullableClass13), "Field2", "arg5", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass13), "Field2", "arg6", typeof(IEnumerable<string>), Nullability.NonNullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass13), "Field2", "arg7", typeof(IEnumerable<string>), Nullability.NonNullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass13), "Field2", "arg8", typeof(IEnumerable<string>), Nullability.Nullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass13), "Field2", "arg9", typeof(IEnumerable<string>), Nullability.Nullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass14), "Field2", "arg1", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass14), "Field2", "arg2", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass14), "Field2", "arg3", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass14), "Field2", "arg4", typeof(int), Nullability.Nullable)]
        [InlineData(typeof(NullableClass14), "Field2", "arg5", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass14), "Field2", "arg6", typeof(IEnumerable<string>), Nullability.NonNullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass14), "Field2", "arg7", typeof(IEnumerable<string>), Nullability.NonNullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass14), "Field2", "arg8", typeof(IEnumerable<string>), Nullability.Nullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass14), "Field2", "arg9", typeof(IEnumerable<string>), Nullability.Nullable, typeof(string), Nullability.Nullable)]
        public void Argument(Type type, string methodName, string argumentName, Type expectedType, Nullability expectedNullability, Type expectedType2 = null, Nullability? expectedNullability2 = null)
        {
            var method = type.GetMethod(methodName);
            var argument = method.GetParameters().Single(x => x.Name == argumentName);
            var actual = argument.GetNullabilityInformation().ToList();
            actual.Count.ShouldBe(expectedType2 == null ? 1 : 2);
            actual[0].Type.ShouldBe(expectedType);
            actual[0].Nullable.ShouldBe(expectedNullability);
            if (expectedType2 != null) {
                actual[1].Type.ShouldBe(expectedType2);
                actual[1].Nullable.ShouldBe(expectedNullability2.Value);
            }
        }
    }
}
