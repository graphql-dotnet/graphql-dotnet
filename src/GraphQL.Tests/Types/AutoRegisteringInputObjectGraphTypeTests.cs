#nullable enable

using System.Collections;
using System.ComponentModel;
using System.Reflection;
using GraphQL.Types;

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
    [InlineData(nameof(FieldTests.NotNullArrayNullableTupleField), typeof(NonNullGraphType<ListGraphType<GraphQLClrInputTypeReference<Tuple<int, string>>>>))]
    [InlineData(nameof(FieldTests.NotNullArrayNotNullTupleField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<Tuple<int, string>>>>>))]
    [InlineData(nameof(FieldTests.NullableArrayNullableTupleField), typeof(ListGraphType<GraphQLClrInputTypeReference<Tuple<int, string>>>))]
    [InlineData(nameof(FieldTests.NullableArrayNotNullTupleField), typeof(ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<Tuple<int, string>>>>))]
    [InlineData(nameof(FieldTests.IdField), typeof(NonNullGraphType<IdGraphType>))]
    [InlineData(nameof(FieldTests.NullableIdField), typeof(IdGraphType))]
    [InlineData(nameof(FieldTests.EnumerableField), typeof(NonNullGraphType<ListGraphType<GraphQLClrInputTypeReference<object>>>))]
    [InlineData(nameof(FieldTests.CollectionField), typeof(NonNullGraphType<ListGraphType<GraphQLClrInputTypeReference<object>>>))]
    [InlineData(nameof(FieldTests.NullableEnumerableField), typeof(ListGraphType<GraphQLClrInputTypeReference<object>>))]
    [InlineData(nameof(FieldTests.NullableCollectionField), typeof(ListGraphType<GraphQLClrInputTypeReference<object>>))]
    [InlineData(nameof(FieldTests.ListOfListOfIntsField), typeof(ListGraphType<ListGraphType<GraphQLClrInputTypeReference<int>>>))]
    public void Field_DectectsProperType(string fieldName, Type expectedGraphType)
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
            { "Field1", 11 },
            { "Field3", 13 },
            { "Field5", 15 },
            { "Field6AltName", 16 },
        };
        var graph = new TestFieldSupport<TestClass>();
        var actual = graph.ParseDictionary(dic).ShouldBeOfType<TestClass>();
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
    }

    [Fact]
    public void TestBasicRecordNoExtraFields()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestBasicRecord>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);
    }

    [Fact]
    public void TestBasicRecordStructNoExtraFields()
    {
        var graphType = new AutoRegisteringInputObjectGraphType<TestBasicRecordStruct>();
        graphType.Fields.Find("Id").ShouldNotBeNull();
        graphType.Fields.Find("Name").ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(2);
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
        [InputType(typeof(IdGraphType))]
        public int? Field6 { get; set; }
        [OutputType(typeof(IdGraphType))]
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
        public Tuple<int, string>?[] NotNullArrayNullableTupleField { get; set; } = null!;
        public Tuple<int, string>[] NotNullArrayNotNullTupleField { get; set; } = null!;
        public Tuple<int, string>?[]? NullableArrayNullableTupleField { get; set; }
        public Tuple<int, string>[]? NullableArrayNotNullTupleField { get; set; }
        [Id]
        public int IdField { get; set; }
        [Id]
        public int? NullableIdField { get; set; }
        public IEnumerable EnumerableField { get; set; } = null!;
        public ICollection CollectionField { get; set; } = null!;
        public IEnumerable? NullableEnumerableField { get; set; }
        public ICollection? NullableCollectionField { get; set; }
        public int?[]?[]? ListOfListOfIntsField { get; set; }
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

    private class TestBasicClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
