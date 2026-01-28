using System.Collections;
using System.ComponentModel;
using System.Reflection;
using GraphQL.Federation.Types;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Types;

public class AutoRegisteringInputObjectGraphTypeTests
{
    [Fact]
    public void Class_RecognizesNameAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestClass_WithCustomName>();
        graphType.Name.ShouldBe("TestWithCustomName");
    }

    [Fact]
    public void Class_RecognizesInputNameAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestClass_WithCustomInputName>();
        graphType.Name.ShouldBe("TestWithCustomName");
    }

    [Fact]
    public void Class_IgnoresOutputNameAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestClass_WithCustomOutputName>();
        graphType.Name.ShouldBe("TestClass_WithCustomOutputName");
    }

    [Fact]
    public void Class_RecognizesDescriptionAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestClass_WithCustomDescription>();
        graphType.Description.ShouldBe("Test description");
    }

    [Fact]
    public void Class_RecognizesObsoleteAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestClass_WithCustomDeprecationReason>();
        graphType.DeprecationReason.ShouldBe("Test deprecation reason");
    }

    [Fact]
    public void Class_RecognizesCustomGraphQLAttributes()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestClass_WithCustomAttributes>();
        graphType.Description.ShouldBe("Test custom description");
    }

    [Fact]
    public void Class_RecognizesMultipleAttributes()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestClass_WithMultipleAttributes>();
        graphType.Description.ShouldBe("Test description");
        graphType.GetMetadata<string>("key1").ShouldBe("value1");
        graphType.GetMetadata<string>("key2").ShouldBe("value2");
    }

    [Fact]
    public void Class_CanOverrideDefaultName()
    {
        var graphType = new TestOverrideDefaultName<TestClass>();
        graphType.Name.ShouldBe("TestClassInput");
    }

    [Fact]
    public void Class_AttributesApplyOverOverriddenDefaultName()
    {
        var graphType = new TestOverrideDefaultName<TestClass_WithCustomName>();
        graphType.Name.ShouldBe("TestWithCustomName");
    }

    [Fact]
    public void Class_RecognizesInheritedAttributes()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<DerivedClass>();
        graphType.Fields.Find("Field1CustomName").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesNameAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        graphType.Fields.Find("Test1").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesDescriptionAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field2").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test description");
    }

    [Fact]
    public void Field_RecognizesObsoleteAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field3").ShouldNotBeNull();
        fieldType.DeprecationReason.ShouldBe("Test deprecation reason");
    }

    [Fact]
    public void Field_RecognizesCustomGraphQLAttributes()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field4").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test custom description for field");
    }

    [Fact]
    public void Field_RecognizesMultipleAttributes()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field5").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test description");
        fieldType.GetMetadata<string>("key1").ShouldBe("value1");
        fieldType.GetMetadata<string>("key2").ShouldBe("value2");
    }

    [Fact]
    public void Field_RecognizesInputTypeAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field6").ShouldNotBeNull();
        fieldType.Type.ShouldBe(typeof(IdGraphType));
    }

    [Fact]
    public void Field_IgnoresOutputTypeAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field7").ShouldNotBeNull();
        fieldType.Type.ShouldBe(typeof(GraphQLClrInputTypeReference<int>));
    }

    [Fact]
    public void Field_RecognizesDefaultValueAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find("Field8").ShouldNotBeNull();
        fieldType.DefaultValue.ShouldBeOfType<string>().ShouldBe("hello");
    }

    [Fact]
    public void Field_RecognizesInputNameAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        graphType.Fields.Find("InputField9").ShouldNotBeNull();
    }

    [Fact]
    public void Field_IgnoresOutputNameAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        graphType.Fields.Find("Field10").ShouldNotBeNull();
    }

    [Fact]
    public void Field_RecognizesIgnoreAttribute()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        graphType.Fields.Find("Field11").ShouldBeNull();
    }

    [Theory]
    [InlineData(nameof(FieldTests.NotNullIntField), typeof(NonNullGraphType<GraphQLClrInputTypeReference<int>>))]
    [InlineData(nameof(FieldTests.NullableIntField), typeof(GraphQLClrInputTypeReference<int>))]
    [InlineData(nameof(FieldTests.NotNullStringField), typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(FieldTests.NullableStringField), typeof(GraphQLClrInputTypeReference<string>))]
    [InlineData(nameof(FieldTests.NotNullStringWriteOnlyField), typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(FieldTests.NullableStringWriteOnlyField), typeof(GraphQLClrInputTypeReference<string>))]
    [InlineData(nameof(FieldTests.NotNullListNullableStringField), typeof(NonNullGraphType<ListGraphType<GraphQLClrInputTypeReference<string>>>))]
    [InlineData(nameof(FieldTests.NotNullListNotNullStringField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<string>>>>))]
    [InlineData(nameof(FieldTests.NullableListNullableStringField), typeof(ListGraphType<GraphQLClrInputTypeReference<string>>))]
    [InlineData(nameof(FieldTests.NullableListNotNullStringField), typeof(ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<string>>>))]
    [InlineData(nameof(FieldTests.NotNullEnumerableNullableIntField), typeof(NonNullGraphType<ListGraphType<GraphQLClrInputTypeReference<int>>>))]
    [InlineData(nameof(FieldTests.NotNullEnumerableNotNullIntField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<int>>>>))]
    [InlineData(nameof(FieldTests.NullableEnumerableNullableIntField), typeof(ListGraphType<GraphQLClrInputTypeReference<int>>))]
    [InlineData(nameof(FieldTests.NullableEnumerableNotNullIntField), typeof(ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<int>>>))]
    [InlineData(nameof(FieldTests.NotNullArrayNullableTupleField), typeof(NonNullGraphType<ListGraphType<GraphQLClrInputTypeReference<InputTuple<int, string>>>>))]
    [InlineData(nameof(FieldTests.NotNullArrayNotNullTupleField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<InputTuple<int, string>>>>>))]
    [InlineData(nameof(FieldTests.NullableArrayNullableTupleField), typeof(ListGraphType<GraphQLClrInputTypeReference<InputTuple<int, string>>>))]
    [InlineData(nameof(FieldTests.NullableArrayNotNullTupleField), typeof(ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<InputTuple<int, string>>>>))]
    [InlineData(nameof(FieldTests.IdField), typeof(NonNullGraphType<IdGraphType>))]
    [InlineData(nameof(FieldTests.NullableIdField), typeof(IdGraphType))]
    [InlineData(nameof(FieldTests.EnumerableField), typeof(NonNullGraphType<ListGraphType<GraphQLClrInputTypeReference<object>>>))]
    [InlineData(nameof(FieldTests.CollectionField), typeof(NonNullGraphType<ListGraphType<GraphQLClrInputTypeReference<object>>>))]
    [InlineData(nameof(FieldTests.NullableEnumerableField), typeof(ListGraphType<GraphQLClrInputTypeReference<object>>))]
    [InlineData(nameof(FieldTests.NullableCollectionField), typeof(ListGraphType<GraphQLClrInputTypeReference<object>>))]
    [InlineData(nameof(FieldTests.ListOfListOfIntsField), typeof(ListGraphType<ListGraphType<GraphQLClrInputTypeReference<int>>>))]
    public void Field_DetectsProperType(string fieldName, Type expectedGraphType)
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        var fieldType = graphType.Fields.Find(fieldName).ShouldNotBeNull();
        fieldType.Type.ShouldBe(expectedGraphType);
    }

    [Fact]
    public void DefaultServiceProvider_Should_Create_AutoRegisteringGraphTypes()
    {
        var provider = new DefaultServiceProvider();
        provider.GetService(typeof(AutoRegisteringInputObjectGraphType<TestClass>)).ShouldNotBeNull();
    }

    [Fact]
    public void RegistersWritablePropertiesOnly()
    {
        var inputType = new AutoRegisteringInputObjectGraphType<TestClass>();
        inputType.Fields.Find("Field1").ShouldNotBeNull();
        inputType.Fields.Find("Field2").ShouldBeNull();
        inputType.Fields.Find("Field3").ShouldNotBeNull();
        inputType.Fields.Find("Field4").ShouldBeNull();
        inputType.Fields.Find("Field5").ShouldBeNull();
    }

    [Fact]
    public void SkipsSpecifiedProperties()
    {
        var inputType = new AutoRegisteringInputObjectGraphType<TestClass>(x => x.Field1);
        inputType.Fields.Find("Field1").ShouldBeNull();
        inputType.Fields.Find("Field2").ShouldBeNull();
        inputType.Fields.Find("Field3").ShouldNotBeNull();
        inputType.Fields.Find("Field4").ShouldBeNull();
        inputType.Fields.Find("Field5").ShouldBeNull();
    }

    [Fact]
    public void CanOverrideFieldGeneration()
    {
        var graph = new TestChangingName<TestClass>();
        graph.Fields.Find("Field1Prop").ShouldNotBeNull();
    }

    [Fact]
    public void CanOverrideFieldList()
    {
        var graph = new TestChangingFieldList<TestClass>();
        graph.Fields.Count.ShouldBe(1);
        graph.Fields.Find("Field1").ShouldNotBeNull();
    }

    [Fact]
    public void CanAddFieldInfos()
    {
        var graph = new TestFieldSupport<TestClass>();
        graph.Fields.Find("Field5").ShouldNotBeNull();
    }

    [Fact]
    public void DoesParse()
    {
        var dic = new Dictionary<string, object?>()
        {
            { "field1", 11 },
            { "field3", 13 },
            { "field5", 15 },
            { "field6AltName", 16 },
        };
        var graph = new TestFieldSupport<TestClass>();
        var actual = TestParse<TestClass>(graph, dic);
        actual.Field1.ShouldBe(11);
        actual.Field3Value.ShouldBe(13);
        actual.Field5.ShouldBe(15);
        actual.Field6.ShouldBe(16);
    }

    [Fact]
    public void TestBasicClassNoExtraFields()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestBasicClass>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);

        var actual = TestParse<TestBasicClass>(graphType, """{"id":123,"name":"John Doe"}""");
        actual.Id.ShouldBe(123);
        actual.Name.ShouldBe("John Doe");
    }

    [Fact]
    public void TestBasicRecordNoExtraFields()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestBasicRecord>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);

        var actual = TestParse<TestBasicRecord>(graphType, """{"id":123,"name":"John Doe"}""");
        actual.Id.ShouldBe(123);
        actual.Name.ShouldBe("John Doe");
    }

    [Fact]
    public void TestBasicRecordStructNoExtraFields()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestBasicRecordStruct>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);

        var actual = TestParse<TestBasicRecordStruct>(graphType, """{"id":123,"name":"John Doe"}""");
        actual.Id.ShouldBe(123);
        actual.Name.ShouldBe("John Doe");
    }

    [Fact]
    public void TestWritableStruct()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestStructWritableProperties>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);

        var actual = TestParse<TestStructWritableProperties>(graphType, """{"id":123,"name":"John Doe"}""");
        actual.Id.ShouldBe(123);
        actual.Name.ShouldBe("John Doe");
    }

    [Fact]
    public void TestReadOnlyStruct()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestStructReadOnlyProperties>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);

        var actual = TestParse<TestStructReadOnlyProperties>(graphType, """{"id":123,"name":"John Doe"}""");
        actual.Id.ShouldBe(123);
        actual.Name.ShouldBe("John Doe");
    }

    [Fact]
    public void RegistersInitFields()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<FieldTests>();
        graphType.Fields.Find(nameof(FieldTests.FieldWithInitSetter)).ShouldNotBeNull();

        var actual = TestParse<FieldTests>(graphType, """{"fieldWithInitSetter":"hello"}""");
        actual.FieldWithInitSetter.ShouldBe("hello");
    }

    [Fact]
    public void RegistersConstructorProperties()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<ReadOnlyClass>();
        var idField = graphType.Fields.Find(nameof(ReadOnlyClass.Id)).ShouldNotBeNull();
        idField.Name.ShouldBe("Id");
        idField.Type.ShouldBe(typeof(NonNullGraphType<IdGraphType>));
        var nameField = graphType.Fields.Find(nameof(ReadOnlyClass.Name)).ShouldNotBeNull();
        nameField.Name.ShouldBe("Name");
        nameField.Type.ShouldBe(typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>));

        var actual = TestParse<ReadOnlyClass>(graphType, """{"id":123,"name":"John Doe"}""");
        actual.Id.ShouldBe(123);
        actual.Name.ShouldBe("John Doe");
    }

    [Fact]
    public void ParserSetProperly()
    {
        var inputType = new AutoRegisteringInputObjectGraphType<Class3>();
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<StringGraphType>("test")
            .Argument<int>("input", configure: arg => arg.ResolvedType = inputType);
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        // verify that during input coercion, the value is converted to an integer
        inputType.Fields.First().ResolvedType.ShouldBeOfType<NonNullGraphType>().ResolvedType.ShouldBeOfType<IdGraphType>();
        inputType.Fields.First().Parser.ShouldNotBeNull().Invoke("123", schema.ValueConverter).ShouldBe(123);
        // verify that during input coercion, parsing errors throw an exception
        Should.Throw<FormatException>(() => inputType.Fields.First().Parser.ShouldNotBeNull().Invoke("abc", schema.ValueConverter));
    }

    private class Class3
    {
        [Id]
        public int Id { get; set; }
    }

    private class Class1
    {
        public string? Sample { get; set; }
    }

    private class FieldTests
    {
        [Name("Test1")]
        public string? Field1 { get; set; }
        [Description("Test description")]
        public string? Field2 { get; set; }
        [Obsolete("Test deprecation reason")]
        public string? Field3 { get; set; }
        [CustomDescription]
        public string? Field4 { get; set; }
        [Description("Test description")]
        [Metadata("key1", "value1")]
        [Metadata("key2", "value2")]
        public string? Field5 { get; set; }
#if NET48
        [InputType(typeof(IdGraphType))]
#else
        [InputType<IdGraphType>()]
#endif
        public int? Field6 { get; set; }
#if NET48
        [OutputType(typeof(IdGraphType))]
#else
        [OutputType<IdGraphType>()]
#endif
        public int? Field7 { get; set; }
        [DefaultValue("hello")]
        public string? Field8 { get; set; }
        [InputName("InputField9")]
        public string? Field9 { get; set; }
        [OutputName("OutputField10")]
        public string? Field10 { get; set; }
        [Ignore]
        public string? Field11 { get; set; }
        public int NotNullIntField { get; set; }
        public int? NullableIntField { get; set; }
        public string NotNullStringField { get; set; } = null!;
        public string? NullableStringField { get; set; }
        public string NotNullStringWriteOnlyField { set { } }
        public string? NullableStringWriteOnlyField { set { } }
        public List<string?> NotNullListNullableStringField { get; set; } = null!;
        public List<string> NotNullListNotNullStringField { get; set; } = null!;
        public List<string?>? NullableListNullableStringField { get; set; }
        public List<string>? NullableListNotNullStringField { get; set; }
        public IEnumerable<int?> NotNullEnumerableNullableIntField { get; set; } = null!;
        public IEnumerable<int> NotNullEnumerableNotNullIntField { get; set; } = null!;
        public IEnumerable<int?>? NullableEnumerableNullableIntField { get; set; }
        public IEnumerable<int>? NullableEnumerableNotNullIntField { get; set; }
        public InputTuple<int, string>?[] NotNullArrayNullableTupleField { get; set; } = null!;
        public InputTuple<int, string>[] NotNullArrayNotNullTupleField { get; set; } = null!;
        public InputTuple<int, string>?[]? NullableArrayNullableTupleField { get; set; }
        public InputTuple<int, string>[]? NullableArrayNotNullTupleField { get; set; }
        [Id]
        public int IdField { get; set; }
        [Id]
        public int? NullableIdField { get; set; }
        public IEnumerable EnumerableField { get; set; } = null!;
        public ICollection CollectionField { get; set; } = null!;
        public IEnumerable? NullableEnumerableField { get; set; }
        public ICollection? NullableCollectionField { get; set; }
        public int?[]?[]? ListOfListOfIntsField { get; set; }
        public string FieldWithInitSetter { get; init; } = null!;
    }

    private class InputTuple<T1, T2>
    {
        public T1? Item1 { get; set; }
        public T2? Item2 { get; set; }
    }

    private class TestChangingFieldList<T> : AutoRegisteringInputObjectGraphType<T>
    {
        protected override IEnumerable<FieldType> ProvideFields()
        {
            yield return CreateField(GetRegisteredMembers().First(x => x.Name == "Field1"))!;
        }
    }

    private class TestFieldSupport<T> : AutoRegisteringInputObjectGraphType<T>
    {
        protected override IEnumerable<MemberInfo> GetRegisteredMembers()
            => base.GetRegisteredMembers().Concat(typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public));
    }

    private class TestChangingName<T> : AutoRegisteringInputObjectGraphType<T>
    {
        protected override FieldType CreateField(MemberInfo memberInfo)
        {
            var field = base.CreateField(memberInfo)!;
            field.Name += "Prop";
            return field;
        }
    }

    private class TestClass
    {
        internal int Field3Value = 0;
        public int Field1 { get; set; } = 1;
        public int Field2 { get; } = 2;
        public int Field3 { set => Field3Value = value; }
        public int Field4() => 4;
        public int Field5 = 5;
        [Name("Field6AltName")]
        public int Field6 { get; set; } = 6;
    }

    [Name("TestWithCustomName")]
    private class TestClass_WithCustomName { }

    [InputName("TestWithCustomName")]
    private class TestClass_WithCustomInputName { }

    [OutputName("TestWithCustomName")]
    private class TestClass_WithCustomOutputName { }

    [Description("Test description")]
    private class TestClass_WithCustomDescription { }

    [Obsolete("Test deprecation reason")]
    private class TestClass_WithCustomDeprecationReason { }

    [CustomDescription]
    private class TestClass_WithCustomAttributes { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    private class CustomDescriptionAttribute : GraphQLAttribute
    {
        public override void Modify(IGraphType graphType)
        {
            graphType.Description = "Test custom description";
        }

        public override void Modify(FieldType fieldType, bool isInputType)
        {
            fieldType.Description = "Test custom description for field";
        }
    }

    [Metadata("key1", "value1")]
    [Metadata("key2", "value2")]
    [Description("Test description")]
    private class TestClass_WithMultipleAttributes { }

    private class TestOverrideDefaultName<T> : AutoRegisteringInputObjectGraphType<T>
    {
        protected override void ConfigureGraph()
        {
            Name = typeof(T).Name + "Input";
            base.ConfigureGraph();
        }
    }

    private class ParentClass
    {
        [Name("Field1CustomName")]
        public virtual string? Field1 { get; set; }
    }
    private class DerivedClass : ParentClass
    {
        public override string? Field1 { get => base.Field1; set => base.Field1 = value; }
    }

    private record TestBasicRecord(int Id, string Name);

    private record struct TestBasicRecordStruct(int Id, string Name);

    private struct TestStructWritableProperties
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private struct TestStructReadOnlyProperties
    {
        public int Id { get; }
        public string Name { get; }

        public TestStructReadOnlyProperties(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    private class TestBasicClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    private class ReadOnlyClass
    {
        public ReadOnlyClass(int id, string name)
        {
            Id = id;
            Name = name;
        }

        [Id]
        public int Id { get; }
        public string Name { get; }
    }

    private T TestParse<T>(IInputObjectGraphType graphType, string inputJson)
        => TestParse<T>(graphType, inputJson.ToDictionary());

    private T TestParse<T>(IInputObjectGraphType graphType, Dictionary<string, object?> dictionary)
    {
        var queryType = new ObjectGraphType();
        queryType.Field<StringGraphType>("test");
        using var provider = new ServiceCollection()
            .AddSingleton<AnyScalarGraphType>()
            .AddGraphQL(b => b
                .AddAutoSchema<Class1>()
                .ConfigureSchema(s =>
                {
                    s.RegisterType(graphType);
                    s.RegisterTypeMapping<object, AnyScalarGraphType>();
                }))
            .BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        return graphType.ParseDictionary(dictionary, schema.ValueConverter).ShouldBeOfType<T>();
    }

    [Fact]
    public void MemberScanAttribute_InputType_PropertiesOnly()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<InputPropertiesOnlyClass>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Property2").ShouldNotBeNull();
        graphType.Fields.Find("Field1").ShouldBeNull();
    }

    [Fact]
    public void MemberScanAttribute_InputType_FieldsOnly()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<InputFieldsOnlyClass>();
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("Field2").ShouldNotBeNull();
    }

    [Fact]
    public void MemberScanAttribute_InputType_FieldsOnlyDerivedClass()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<InputFieldsOnlyDerivedClass>();
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("Property2").ShouldBeNull();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("Field2").ShouldNotBeNull();
        graphType.Fields.Find("Field3").ShouldNotBeNull();
    }

    [Fact]
    public void MemberScanAttribute_InputType_PropertiesAndFields()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<InputPropertiesAndFieldsClass>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
    }

    [Fact]
    public void MemberScanAttribute_InputType_MethodsAreSkipped()
    {
        // Methods should be silently ignored for input types, so when Methods-only is specified,
        // no fields will be present (since properties are not included in the scan)
        var graphType = new AutoRegisteringInputObjectGraphType<InputMethodsClass>();
        graphType.Fields.Count.ShouldBe(0); // No fields should be present
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("GetData").ShouldBeNull(); // Method should be skipped
    }

    [Fact]
    public void MemberScanAttribute_InputType_PropertiesAndMethodsSkipsMethods()
    {
        // When both Properties and Methods are specified, methods should be silently ignored
        var graphType = new AutoRegisteringInputObjectGraphType<InputPropertiesAndMethodsClass>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Property2").ShouldNotBeNull();
        graphType.Fields.Find("GetData").ShouldBeNull(); // Method should be skipped
        graphType.Fields.Find("Field1").ShouldBeNull(); // Field should not be included
    }

    [Fact]
    public void MemberScanAttribute_InputType_AllMembersSkipsMethods()
    {
        // When all member types are specified, methods should be silently ignored
        var graphType = new AutoRegisteringInputObjectGraphType<InputAllMembersClass>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("GetData").ShouldBeNull(); // Method should be skipped
    }

    [Fact]
    public void MemberScanAttribute_InputType_Struct_PropertiesOnly()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<InputPropertiesOnlyStruct>();
        graphType.Fields.Find("Property1").ShouldNotBeNull();
        graphType.Fields.Find("Property2").ShouldNotBeNull();
        graphType.Fields.Find("Field1").ShouldBeNull();
    }

    [Fact]
    public void MemberScanAttribute_InputType_Struct_FieldsOnly()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<InputFieldsOnlyStruct>();
        graphType.Fields.Find("Property1").ShouldBeNull();
        graphType.Fields.Find("Field1").ShouldNotBeNull();
        graphType.Fields.Find("Field2").ShouldNotBeNull();
    }

    [MemberScan(ScanMemberTypes.Properties)]
    private class InputPropertiesOnlyClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Property2 { get; set; } = "prop2";
        public string Field1 = "field1";
    }

    [MemberScan(ScanMemberTypes.Fields)]
    private class InputFieldsOnlyClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Field1 = "field1";
        public string Field2 = "field2";
    }

    [MemberScan(ScanMemberTypes.Properties | ScanMemberTypes.Fields)]
    private class InputPropertiesAndFieldsClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Field1 = "field1";
    }

    [MemberScan(ScanMemberTypes.Methods)]
    private class InputMethodsClass
    {
        public string Property1 { get; set; } = "prop1";
        public string GetData() => "data";
    }

    [MemberScan(ScanMemberTypes.Properties | ScanMemberTypes.Methods)]
    private class InputPropertiesAndMethodsClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Property2 { get; set; } = "prop2";
        public string Field1 = "field1";
        public string GetData() => "data";
    }

    [MemberScan(ScanMemberTypes.Properties | ScanMemberTypes.Fields | ScanMemberTypes.Methods)]
    private class InputAllMembersClass
    {
        public string Property1 { get; set; } = "prop1";
        public string Field1 = "field1";
        public string GetData() => "data";
    }

    [MemberScan(ScanMemberTypes.Fields)]
    private class InputFieldsOnlyDerivedClass : InputFieldsOnlyClass
    {
        public string Property2 { get; set; } = "prop1";
        public string Field3 = "field1";
    }

    [MemberScan(ScanMemberTypes.Properties)]
    private struct InputPropertiesOnlyStruct
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
        public string Field1;

        public InputPropertiesOnlyStruct()
        {
            Property1 = "prop1";
            Property2 = "prop2";
            Field1 = "field1";
        }
    }

    [MemberScan(ScanMemberTypes.Fields)]
    private struct InputFieldsOnlyStruct
    {
        public string Property1 { get; set; }
        public string Field1;
        public string Field2;

        public InputFieldsOnlyStruct()
        {
            Property1 = "prop1";
            Field1 = "field1";
            Field2 = "field2";
        }
    }

}
