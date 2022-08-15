using System.Collections;
using System.Numerics;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Utilities;

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
    [InlineData(typeof(object), true, TypeMappingMode.UseBuiltInScalarMappings, null)]
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
    [InlineData(typeof(object), false, TypeMappingMode.UseBuiltInScalarMappings, null)]
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
    [InlineData(typeof(object), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<object>))]
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
    [InlineData(typeof(object), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<object>>))]
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
    [InlineData(typeof(IDictionary<int, string>), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<IDictionary<int, string>>))]
    [InlineData(typeof(IEnumerable<KeyValuePair<int, string>>), true, TypeMappingMode.OutputType, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<KeyValuePair<int, string>>>>))]
    [InlineData(typeof(IEnumerable<int>), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>>))]
    //input mapping mode
    [InlineData(typeof(int), true, TypeMappingMode.InputType, typeof(GraphQLClrInputTypeReference<int>))]
    //data loader compatibility
    [InlineData(typeof(IDataLoaderResult), true, TypeMappingMode.UseBuiltInScalarMappings, null)]
    [InlineData(typeof(IDataLoaderResult), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<object>))]
    [InlineData(typeof(IDataLoaderResult), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<object>>))]
    [InlineData(typeof(IDataLoaderResult<string>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(StringGraphType))]
    [InlineData(typeof(IDataLoaderResult<string>), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(IDataLoaderResult<string>), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<string>))]
    [InlineData(typeof(IDataLoaderResult<string>), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData(typeof(IDataLoaderResult<int>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(IntGraphType))]
    [InlineData(typeof(IDataLoaderResult<int>), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<IntGraphType>))]
    [InlineData(typeof(IDataLoaderResult<int>), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<int>))]
    [InlineData(typeof(IDataLoaderResult<int>), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
    [InlineData(typeof(IDataLoaderResult<int?>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(IntGraphType))]
    [InlineData(typeof(IDataLoaderResult<int?>), false, TypeMappingMode.UseBuiltInScalarMappings, null)]
    [InlineData(typeof(IDataLoaderResult<int?>), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<int>))]
    [InlineData(typeof(IDataLoaderResult<int?>), false, TypeMappingMode.OutputType, null)]
    [InlineData(typeof(IDataLoaderResult<IDataLoaderResult<string>>), true, TypeMappingMode.UseBuiltInScalarMappings, typeof(StringGraphType))]
    [InlineData(typeof(IDataLoaderResult<IDataLoaderResult<int>>), false, TypeMappingMode.UseBuiltInScalarMappings, typeof(NonNullGraphType<IntGraphType>))]
    [InlineData(typeof(IDataLoaderResult<IDataLoaderResult<int>>), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
    [InlineData(typeof(IDataLoaderResult<IEnumerable<IDataLoaderResult<int>>>), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<int>>>))]
    //task incompatibility
    [InlineData(typeof(Task), true, TypeMappingMode.UseBuiltInScalarMappings, null)]
    [InlineData(typeof(Task), true, TypeMappingMode.OutputType, null)]
    [InlineData(typeof(Task<string>), true, TypeMappingMode.UseBuiltInScalarMappings, null)]
    [InlineData(typeof(Task<string>), true, TypeMappingMode.OutputType, null)]
    [InlineData(typeof(AttributeTest1), true, TypeMappingMode.InputType, typeof(CustomInputGraphType))]
    [InlineData(typeof(AttributeTest1), false, TypeMappingMode.InputType, typeof(NonNullGraphType<CustomInputGraphType>))]
    [InlineData(typeof(AttributeTest2), true, TypeMappingMode.InputType, typeof(CustomInputGraphType))]
    [InlineData(typeof(AttributeTest2), false, TypeMappingMode.InputType, typeof(NonNullGraphType<CustomInputGraphType>))]
    [InlineData(typeof(AttributeTest3), true, TypeMappingMode.InputType, typeof(GraphQLClrInputTypeReference<AttributeTest3>))]
    [InlineData(typeof(AttributeTest3), false, TypeMappingMode.InputType, typeof(NonNullGraphType<GraphQLClrInputTypeReference<AttributeTest3>>))]
    [InlineData(typeof(AttributeTest1), true, TypeMappingMode.OutputType, typeof(CustomOutputGraphType))]
    [InlineData(typeof(AttributeTest1), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<CustomOutputGraphType>))]
    [InlineData(typeof(AttributeTest2), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<AttributeTest2>))]
    [InlineData(typeof(AttributeTest2), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<AttributeTest2>>))]
    [InlineData(typeof(AttributeTest3), true, TypeMappingMode.OutputType, typeof(CustomOutputGraphType))]
    [InlineData(typeof(AttributeTest3), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<CustomOutputGraphType>))]
    [InlineData(typeof(AttributeTest4), true, TypeMappingMode.InputType, typeof(CustomInputGraphType))]
    [InlineData(typeof(AttributeTest4), false, TypeMappingMode.InputType, typeof(NonNullGraphType<CustomInputGraphType>))]
    [InlineData(typeof(AttributeTest5), true, TypeMappingMode.InputType, typeof(CustomInputGraphType))]
    [InlineData(typeof(AttributeTest5), false, TypeMappingMode.InputType, typeof(NonNullGraphType<CustomInputGraphType>))]
    [InlineData(typeof(AttributeTest6), true, TypeMappingMode.InputType, typeof(GraphQLClrInputTypeReference<AttributeTest6>))]
    [InlineData(typeof(AttributeTest6), false, TypeMappingMode.InputType, typeof(NonNullGraphType<GraphQLClrInputTypeReference<AttributeTest6>>))]
    [InlineData(typeof(AttributeTest4), true, TypeMappingMode.OutputType, typeof(CustomOutputGraphType))]
    [InlineData(typeof(AttributeTest4), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<CustomOutputGraphType>))]
    [InlineData(typeof(AttributeTest5), true, TypeMappingMode.OutputType, typeof(GraphQLClrOutputTypeReference<AttributeTest5>))]
    [InlineData(typeof(AttributeTest5), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<AttributeTest5>>))]
    [InlineData(typeof(AttributeTest6), true, TypeMappingMode.OutputType, typeof(CustomOutputGraphType))]
    [InlineData(typeof(AttributeTest6), false, TypeMappingMode.OutputType, typeof(NonNullGraphType<CustomOutputGraphType>))]
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

    [Theory]
    [InlineData(typeof(IntGraphType), typeof(IntGraphType))]
    [InlineData(typeof(StringGraphType), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<StringGraphType>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<StringGraphType>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(MyClassObjectType), typeof(MyClassObjectType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassObjectType>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassObjectType>>>>))]
    [InlineData(typeof(GraphQLClrOutputTypeReference<int>), typeof(IntGraphType))]
    [InlineData(typeof(GraphQLClrOutputTypeReference<string>), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<GraphQLClrOutputTypeReference<string>>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(GraphQLClrOutputTypeReference<MyClass>), typeof(MyClassObjectType))]
    [InlineData(typeof(GraphQLClrOutputTypeReference<MyEnum>), typeof(EnumerationGraphType<MyEnum>))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<MyClass>>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassObjectType>>>>))]
    public void OutputTypeIsDereferenced(Type referenceType, Type mappedType)
    {
        var query = new ObjectGraphType();
        query.Field("test", referenceType);
        var schema = new Schema
        {
            Query = query
        };
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassObjectType));
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassInputType));
        schema.Initialize();
        schema.Query.Fields.Find("test").Type.ShouldBe(mappedType);
    }

    [Theory]
    [InlineData(typeof(IntGraphType), typeof(IntGraphType))]
    [InlineData(typeof(StringGraphType), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<StringGraphType>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<StringGraphType>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(MyClassInputType), typeof(MyClassInputType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<int>), typeof(IntGraphType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<string>), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<GraphQLClrInputTypeReference<string>>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyClass>), typeof(MyClassInputType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyEnum>), typeof(EnumerationGraphType<MyEnum>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MappedEnum>), typeof(MappedEnumGraphType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<MyClass>>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    public void InputTypeIsDereferenced_Argument(Type referenceType, Type mappedType)
    {
        var query = new ObjectGraphType();
        query.Field("test", typeof(IntGraphType))
            .Argument(referenceType, "arg");
        var schema = new Schema
        {
            Query = query
        };
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassObjectType));
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassInputType));
        schema.RegisterTypeMapping(typeof(MappedEnum), typeof(MappedEnumGraphType));
        schema.Initialize();
        schema.Query.Fields.Find("test").Arguments.Find("arg").Type.ShouldBe(mappedType);
    }

    [Theory]
    [InlineData(typeof(IntGraphType), typeof(IntGraphType))]
    [InlineData(typeof(StringGraphType), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<StringGraphType>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<StringGraphType>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(MyClassInputType), typeof(MyClassInputType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<int>), typeof(IntGraphType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<string>), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<GraphQLClrInputTypeReference<string>>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyClass>), typeof(MyClassInputType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyEnum>), typeof(EnumerationGraphType<MyEnum>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MappedEnum>), typeof(MappedEnumGraphType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<MyClass>>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    public void InputTypeIsDereferenced_DirectiveArgument(Type referenceType, Type mappedType)
    {
        var query = new ObjectGraphType();
        query.Field("test", typeof(StringGraphType));
        var schema = new Schema
        {
            Query = query
        };
        var directive = new Directive("MyDirective")
        {
            Arguments = new QueryArguments
            {
                new QueryArgument(referenceType) { Name = "arg" }
            }
        };
        directive.Locations.Add(DirectiveLocation.Field);
        schema.Directives.Register(directive);
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassObjectType));
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassInputType));
        schema.RegisterTypeMapping(typeof(MappedEnum), typeof(MappedEnumGraphType));
        schema.Initialize();
        schema.Directives.Find("MyDirective").Arguments.Find("arg").Type.ShouldBe(mappedType);
    }

    [Theory]
    [InlineData(typeof(IntGraphType), typeof(IntGraphType))]
    [InlineData(typeof(StringGraphType), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<StringGraphType>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<StringGraphType>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(MyClassInputType), typeof(MyClassInputType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<int>), typeof(IntGraphType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<string>), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<GraphQLClrInputTypeReference<string>>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyClass>), typeof(MyClassInputType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyEnum>), typeof(EnumerationGraphType<MyEnum>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MappedEnum>), typeof(MappedEnumGraphType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<MyClass>>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    public void InputTypeIsDereferenced_InputField(Type referenceType, Type mappedType)
    {
        var inputType = new InputObjectGraphType();
        inputType.Field("field", referenceType);
        var query = new ObjectGraphType();
        query.Field("test", typeof(IntGraphType))
            .Arguments(new QueryArgument(inputType) { Name = "arg" });
        var schema = new Schema
        {
            Query = query
        };
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassObjectType));
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassInputType));
        schema.RegisterTypeMapping(typeof(MappedEnum), typeof(MappedEnumGraphType));
        schema.Initialize();
        var inputTypeActual = schema.Query.Fields.Find("test").Arguments.Find("arg").ResolvedType.ShouldBeOfType<InputObjectGraphType>();
        inputTypeActual.ShouldBe(inputType);
        inputTypeActual.Fields.Find("field").Type.ShouldBe(mappedType);
    }

    private class MyClassObjectType : ObjectGraphType<MyClass>
    {
        public MyClassObjectType()
        {
            Field<IntGraphType>("field");
        }
    }

    private class MyClassInputType : InputObjectGraphType
    {
        public MyClassInputType()
        {
            Field<IntGraphType>("field");
        }
    }

    private class MyClass
    {
    }

    private enum MyEnum
    {
        Value1
    }

    private enum MappedEnum
    {
        Value1
    }

    private class MappedEnumGraphType : EnumerationGraphType<MappedEnum>
    {
    }

    private class CustomInputGraphType : InputObjectGraphType
    {
    }

    private class CustomOutputGraphType : ObjectGraphType
    {
    }

    [GraphQLMetadata(InputType = typeof(CustomInputGraphType), OutputType = typeof(CustomOutputGraphType))]
    private class AttributeTest1 { }

    [GraphQLMetadata(InputType = typeof(CustomInputGraphType))]
    private class AttributeTest2 { }

    [GraphQLMetadata(OutputType = typeof(CustomOutputGraphType))]
    private class AttributeTest3 { }

    [InputType(typeof(CustomInputGraphType))]
    [OutputType(typeof(CustomOutputGraphType))]
    private class AttributeTest4 { }

    [InputType(typeof(CustomInputGraphType))]
    private class AttributeTest5 { }

    [OutputType(typeof(CustomOutputGraphType))]
    private class AttributeTest6 { }
}
