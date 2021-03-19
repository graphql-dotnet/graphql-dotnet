using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class GetGraphTypeFromTypeTests
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
        public void supports_short_type()
        {
            typeof(short).GetGraphTypeFromType(true).ShouldBe(typeof(ShortGraphType));
        }

        [Fact]
        public void supports_ushort_type()
        {
            typeof(ushort).GetGraphTypeFromType(true).ShouldBe(typeof(UShortGraphType));
        }

        [Fact]
        public void supports_ulong_type()
        {
            typeof(ulong).GetGraphTypeFromType(true).ShouldBe(typeof(ULongGraphType));
        }

        [Fact]
        public void supports_uint_type()
        {
            typeof(uint).GetGraphTypeFromType(true).ShouldBe(typeof(UIntGraphType));
        }

        [Fact]
        public void GetGraphTypeFromType_ForIList_EqualToListGraphType() =>
            typeof(IList<string>).GetGraphTypeFromType(true).ShouldBe(typeof(ListGraphType<StringGraphType>));

        [Fact]
        public void GetGraphTypeFromType_ForIReadOnlyCollection_EqualToListGraphType() =>
            typeof(IReadOnlyCollection<string>).GetGraphTypeFromType(true).ShouldBe(typeof(ListGraphType<StringGraphType>));

        [Fact]
        public void GetGraphTypeFromType_ForIEnumerable_EqualToListGraphType() =>
            typeof(IEnumerable<string>).GetGraphTypeFromType(true).ShouldBe(typeof(ListGraphType<StringGraphType>));

        [Fact]
        public void GetGraphTypeFromType_ForICollection_EqualToListGraphType() =>
            typeof(ICollection<string>).GetGraphTypeFromType(true).ShouldBe(typeof(ListGraphType<StringGraphType>));

        [Fact]
        public void GetGraphTypeFromType_ForList_EqualToListGraphType() =>
            typeof(List<string>).GetGraphTypeFromType(true).ShouldBe(typeof(ListGraphType<StringGraphType>));

        [Fact]
        public void GetGraphTypeFromType_ForArray_EqualToListGraphType() =>
            typeof(string[]).GetGraphTypeFromType(true).ShouldBe(typeof(ListGraphType<StringGraphType>));

        [Fact]
        public void GetGraphTypeFromType_ForString_EqualToStringGraphType() =>
            typeof(string).GetGraphTypeFromType(true).ShouldBe(typeof(StringGraphType));

        [Fact]
        public void GetGraphTypeFromType_ForOpenGeneric_ThrowsArgumentOutOfRangeException() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => typeof(List<>).GetGraphTypeFromType(true));

        [Theory]
        //built-in mapping mode
        [InlineData(typeof(byte), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ByteGraphType))]
        [InlineData(typeof(sbyte), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(SByteGraphType))]
        [InlineData(typeof(short), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ShortGraphType))]
        [InlineData(typeof(ushort), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(UShortGraphType))]
        [InlineData(typeof(int), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(IntGraphType))]
        [InlineData(typeof(uint), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(UIntGraphType))]
        [InlineData(typeof(long), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(LongGraphType))]
        [InlineData(typeof(ulong), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ULongGraphType))]
        [InlineData(typeof(BigInteger), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(BigIntGraphType))]
        [InlineData(typeof(decimal), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(DecimalGraphType))]
        [InlineData(typeof(float), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(FloatGraphType))]
        [InlineData(typeof(double), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(FloatGraphType))]
        [InlineData(typeof(DateTime), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(DateTimeGraphType))]
        [InlineData(typeof(DateTimeOffset), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(DateTimeOffsetGraphType))]
        [InlineData(typeof(TimeSpan), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(TimeSpanSecondsGraphType))]
        [InlineData(typeof(bool), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(BooleanGraphType))]
        [InlineData(typeof(string), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(StringGraphType))]
        [InlineData(typeof(Guid), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(IdGraphType))]
        [InlineData(typeof(Uri), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(UriGraphType))]
        [InlineData(typeof(MyClass), true, TypeMappingMode.UseBuiltInScalarMappings, null)]
        [InlineData(typeof(MyEnum), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(EnumerationGraphType<MyEnum>))]
        //built-in mapping mode - nullable structs
        [InlineData(typeof(int?), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(IntGraphType))]
        [InlineData(typeof(decimal?), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(DecimalGraphType))]
        [InlineData(typeof(Guid?), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(IdGraphType))]
        [InlineData(typeof(MyEnum?), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(EnumerationGraphType<MyEnum>))]
        //built-in mapping mode - not nullable types
        [InlineData(typeof(int), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<IntGraphType>))]
        [InlineData(typeof(string), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<StringGraphType>))]
        [InlineData(typeof(decimal), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<DecimalGraphType>))]
        [InlineData(typeof(Guid), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<IdGraphType>))]
        [InlineData(typeof(Uri), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<UriGraphType>))]
        [InlineData(typeof(MyClass), false, TypeMappingMode.UseBuiltInScalarMappings, null)]
        [InlineData(typeof(MyEnum), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<EnumerationGraphType<MyEnum>>))]
        //built-in mapping mode - nullable structs
        [InlineData(typeof(int?), false, TypeMappingMode.UseBuiltInScalarMappings, null)]
        [InlineData(typeof(decimal?), false, TypeMappingMode.UseBuiltInScalarMappings, null)]
        [InlineData(typeof(Guid?), false, TypeMappingMode.UseBuiltInScalarMappings, null)]
        [InlineData(typeof(MyEnum?), false, TypeMappingMode.UseBuiltInScalarMappings, null)]
        //built-in mapping mode - various types of lists
        [InlineData(typeof(IEnumerable), true, TypeMappingMode.UseBuiltInScalarMappings, null)]
        [InlineData(typeof(IEnumerable<int>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<NonNullGraphType<IntGraphType>>))]
        [InlineData(typeof(IEnumerable<string>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<StringGraphType>))]
        [InlineData(typeof(IEnumerable<decimal>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<NonNullGraphType<DecimalGraphType>>))]
        [InlineData(typeof(IEnumerable<Guid>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<NonNullGraphType<IdGraphType>>))]
        [InlineData(typeof(IEnumerable<Uri>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<UriGraphType>))]
        [InlineData(typeof(IEnumerable<MyClass>), true, TypeMappingMode.UseBuiltInScalarMappings, null)]
        [InlineData(typeof(IEnumerable<MyEnum>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<NonNullGraphType<EnumerationGraphType<MyEnum>>>))]
        [InlineData(typeof(IList<int>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<NonNullGraphType<IntGraphType>>))]
        [InlineData(typeof(ICollection<int>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<NonNullGraphType<IntGraphType>>))]
        [InlineData(typeof(IReadOnlyCollection<int>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<NonNullGraphType<IntGraphType>>))]
        [InlineData(typeof(List<int>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<NonNullGraphType<IntGraphType>>))]
        [InlineData(typeof(int[]), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(ListGraphType<NonNullGraphType<IntGraphType>>))]
        [InlineData(typeof(IDictionary<int, string>), true, TypeMappingMode.UseBuiltInScalarMappings, null)]
        [InlineData(typeof(IEnumerable<int>), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>))]
        [InlineData(typeof(IEnumerable<IEnumerable<int>>), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>>))]
        //output mapping mode
        [InlineData(typeof(int), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<int>))]
        [InlineData(typeof(string), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<string>))]
        [InlineData(typeof(decimal), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<decimal>))]
        [InlineData(typeof(Guid), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<Guid>))]
        [InlineData(typeof(Uri), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<Uri>))]
        [InlineData(typeof(MyClass), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<MyClass>))]
        [InlineData(typeof(MyEnum), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<MyEnum>))]
        //output mapping mode - nullable structs
        [InlineData(typeof(int?), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<int>))]
        [InlineData(typeof(decimal?), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<decimal>))]
        [InlineData(typeof(Guid?), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<Guid>))]
        [InlineData(typeof(MyEnum?), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<MyEnum>))]
        //output mapping mode - not nullable types
        [InlineData(typeof(int), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
        [InlineData(typeof(string), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
        [InlineData(typeof(decimal), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<decimal>>))]
        [InlineData(typeof(Guid), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<Guid>>))]
        [InlineData(typeof(Uri), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<Uri>>))]
        [InlineData(typeof(MyClass), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<MyClass>>))]
        [InlineData(typeof(MyEnum), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<MyEnum>>))]
        //output mapping mode - nullable structs
        [InlineData(typeof(int?), false, TypeMappingMode.OutputType, null)]
        [InlineData(typeof(decimal?), false, TypeMappingMode.OutputType, null)]
        [InlineData(typeof(Guid?), false, TypeMappingMode.OutputType, null)]
        [InlineData(typeof(MyEnum?), false, TypeMappingMode.OutputType, null)]
        //output mapping mode - various types of lists
        [InlineData(typeof(IEnumerable), true, TypeMappingMode.OutputType, typeof(ListGraphType<GraphQLClrOutputTypeReference<object>>))]
        [InlineData(typeof(IEnumerable<int>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
        [InlineData(typeof(IEnumerable<string>), true, TypeMappingMode.OutputType, typeof(ListGraphType<GraphQLClrOutputTypeReference<string>>))]
        [InlineData(typeof(IEnumerable<decimal>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<decimal>>>))]
        [InlineData(typeof(IEnumerable<Guid>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<Guid>>>))]
        [InlineData(typeof(IEnumerable<Uri>), true, TypeMappingMode.OutputType, typeof(ListGraphType<GraphQLClrOutputTypeReference<Uri>>))]
        [InlineData(typeof(IEnumerable<MyClass>), true, TypeMappingMode.OutputType, typeof(ListGraphType<GraphQLClrOutputTypeReference<MyClass>>))]
        [InlineData(typeof(IEnumerable<MyEnum>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<MyEnum>>>))]
        [InlineData(typeof(IList<int>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
        [InlineData(typeof(ICollection<int>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
        [InlineData(typeof(IReadOnlyCollection<int>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
        [InlineData(typeof(List<int>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
        [InlineData(typeof(int[]), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
        [InlineData(typeof(IDictionary<int, string>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<KeyValuePair<int, string>>>>))]
        [InlineData(typeof(IEnumerable<int>), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>>))]
        //input mapping mode
        [InlineData(typeof(int), true, TypeMappingMode.InputType, typeof(GraphQLClrInputTypeReference<int>))]
        public void GetGraphTypeFromType_Matrix(Type type, bool nullable, TypeMappingMode typeMappingMode, Type expectedType)
        {
            if (expectedType == null)
            {
                Should.Throw<ArgumentOutOfRangeException>(() => type.GetGraphTypeFromType(nullable, typeMappingMode));
            }
            else
            {
                type.GetGraphTypeFromType(nullable, typeMappingMode).ShouldBe(expectedType);
            }
        }

        private class MyClass
        {
        }

        private enum MyEnum
        {
        }
    }
}
