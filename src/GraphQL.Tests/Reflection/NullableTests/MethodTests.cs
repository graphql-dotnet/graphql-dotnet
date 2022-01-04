using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Reflection;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Reflection.NullableTests
{
    public class MethodTests
    {
        [Theory]
        [InlineData(typeof(NullableClass1), "Field1", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass1), "Field2", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass1), "Field3", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass1), "Field4", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass1), "Field5", typeof(int), Nullability.Nullable)]
        [InlineData(typeof(NullableClass1), "Field6", typeof(int), Nullability.Nullable)]
        [InlineData(typeof(NullableClass1), "Field7", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass1), "Field8", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass1), "Field9", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass1), "Field10", typeof(List<string>), Nullability.NonNullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass2), "Field1", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass2), "Field2", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass2), "Field3", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass2), "Field4", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass2), "Field5", typeof(int), Nullability.Nullable)]
        [InlineData(typeof(NullableClass2), "Field6", typeof(int), Nullability.Nullable)]
        [InlineData(typeof(NullableClass2), "Field7", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass2), "Field8", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass2), "Field9", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass2), "Field10", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass2), "Field11", typeof(List<string>), Nullability.Nullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass5), "Test", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass6), "Field1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass6), "Field2", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass7), "Field1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass7), "Field2", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass7), "Field3", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass8), "Field1", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass8), "Field2", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass8), "Field3", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass8), "Field4", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass9), "Field1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass10), "Field1", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass11), "Field1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass11), "Field2", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass12), "Field1", typeof(IDataLoaderResult<string>), Nullability.NonNullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass12), "Field2", typeof(IDataLoaderResult<string>), Nullability.NonNullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass12), "Field3", typeof(IDataLoaderResult<string>), Nullability.Nullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass12), "Field4", typeof(IDataLoaderResult<string>), Nullability.Nullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass13), "Field1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass13), "Field2", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass14), "Field1", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass14), "Field2", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass15), "Field1", typeof(Task<string>), Nullability.NonNullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass15), "Field2", typeof(Task<string>), Nullability.NonNullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass15), "Field3", typeof(Task<string>), Nullability.Nullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass15), "Field4", typeof(Task<string>), Nullability.Nullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass16), "Field1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass16), "Field2", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass16), "Field3", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass16.NestedClass1), "Field1", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass16.NestedClass1), "Field2", typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass16.NestedClass1), "Field3", typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass17), "Field1", typeof(Task<string>), Nullability.NonNullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass17), "Field2", typeof(Task<string>), Nullability.NonNullable, typeof(string), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass17), "Field3", typeof(Task<string>), Nullability.NonNullable, typeof(string), Nullability.Nullable)]
        [InlineData(typeof(NullableClass21), "Field1", typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field2", typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field3", typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field4", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass21), "Field5", typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass21), "Field6", typeof(int), Nullability.Nullable)]
        [InlineData(typeof(NullableClass21), "Field7", typeof(int), Nullability.Nullable)]
        [InlineData(typeof(NullableClass21), "Field8", typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field9", typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field10", typeof(List<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field11", typeof(List<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field12", typeof(List<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field13", typeof(List<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field14", typeof(List<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field15", typeof(List<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field16", typeof(List<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field17", typeof(List<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field18", typeof(List<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field19", typeof(IDataLoaderResult<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field20", typeof(IDataLoaderResult<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field21", typeof(Task<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field22", typeof(Task<string>), Nullability.Unknown, typeof(string), Nullability.Unknown)]
        [InlineData(typeof(NullableClass21), "Field23", typeof(List<int>), Nullability.Unknown, typeof(int), Nullability.NonNullable)]
        [InlineData(typeof(NullableClass21), "Field24", typeof(List<int?>), Nullability.Unknown, typeof(int), Nullability.Nullable)]
        public void Method_GetNullability(Type type, string methodName, Type expectedType, Nullability expectedNullability, Type expectedType2 = null, Nullability? expectedNullability2 = null)
        {
            var method = type.GetMethod(methodName);
            var actual = method.ReturnParameter.GetNullabilityInformation().ToList();
            actual.Count.ShouldBe(expectedType2 == null ? 1 : 2);
            actual[0].Type.ShouldBe(expectedType);
            actual[0].Nullable.ShouldBe(expectedNullability);
            if (expectedType2 != null)
            {
                actual[1].Type.ShouldBe(expectedType2);
                actual[1].Nullable.ShouldBe(expectedNullability2.Value);
            }
        }

        public static IEnumerable<object[]> Class18_GetNullability_TestCases()
        {
            return new object[][] {
            new object[] { "Field1", new List<(Type, Nullability)>() {
                (typeof(Tuple<string, string>), Nullability.NonNullable),
                (typeof(string), Nullability.NonNullable),
                (typeof(string), Nullability.Nullable),
            } },
            new object[] { "Field2", new List<(Type, Nullability)>() {
                (typeof(Tuple<Tuple<string, string>, string>), Nullability.NonNullable),
                (typeof(Tuple<string, string>), Nullability.NonNullable),
                (typeof(string), Nullability.Nullable),
                (typeof(string), Nullability.Nullable),
                (typeof(string), Nullability.NonNullable),
            } },
            new object[] { "Field3", new List<(Type, Nullability)>() {
                (typeof(Tuple<int?, string>), Nullability.NonNullable),
                (typeof(int), Nullability.Nullable),
                (typeof(string), Nullability.Nullable),
            } },
            new object[] { "Field4", new List<(Type, Nullability)>() {
                (typeof(Tuple<Guid, string>), Nullability.NonNullable),
                (typeof(Guid), Nullability.NonNullable),
                (typeof(string), Nullability.Nullable),
            } },
            new object[] { "Field5", new List<(Type, Nullability)>() {
                (typeof(Tuple<string[], string>), Nullability.NonNullable),
                (typeof(string[]), Nullability.NonNullable),
                (typeof(string), Nullability.NonNullable),
                (typeof(string), Nullability.Nullable),
            } },
            new object[] { "Field6", new List<(Type, Nullability)>() {
                (typeof(Tuple<int[], string>), Nullability.NonNullable),
                (typeof(int[]), Nullability.NonNullable),
                (typeof(int), Nullability.NonNullable),
                (typeof(string), Nullability.Nullable),
            } },
            new object[] { "Field7", new List<(Type, Nullability)>() {
                (typeof(Tuple<(int, string), string>), Nullability.NonNullable),
                (typeof((int, string)), Nullability.NonNullable),
                (typeof(int), Nullability.NonNullable),
                (typeof(string), Nullability.NonNullable),
                (typeof(string), Nullability.Nullable),
            } },
            new object[] { "Field8", new List<(Type, Nullability)>() {
                (typeof(Tuple<(int, string), string>), Nullability.NonNullable),
                (typeof((int, string)), Nullability.NonNullable),
                (typeof(int), Nullability.NonNullable),
                (typeof(string), Nullability.NonNullable),
                (typeof(string), Nullability.Nullable),
            } },
            new object[] { "Field9", new List<(Type, Nullability)>() {
                (typeof(Tuple<TestStruct<Guid>, string>), Nullability.NonNullable),
                (typeof(TestStruct<Guid>), Nullability.NonNullable),
                (typeof(Guid), Nullability.NonNullable),
                (typeof(string), Nullability.Nullable),
            } },
            new object[] { "Field10", new List<(Type, Nullability)>() {
                (typeof(Tuple<,>).MakeGenericType(typeof(NullableClass18<>).GetGenericArguments()[0], typeof(string)), Nullability.NonNullable),
                (typeof(NullableClass18<>).GetGenericArguments()[0], Nullability.NonNullable),
                (typeof(string), Nullability.Nullable),
            } },
        };
        }

        [Theory]
        [MemberData(nameof(Class18_GetNullability_TestCases))]
        public void Method_GetNullability_Class18(string methodName, List<(Type, Nullability)> expected)
        {
            var method = typeof(NullableClass18<>).GetMethod(methodName).ShouldNotBeNull();
            var actual = method.ReturnParameter.GetNullabilityInformation().ToList();
            expected.ShouldBe(actual);
        }
    }
}
