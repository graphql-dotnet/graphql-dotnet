#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class AutoRegisteringObjectGraphTypeTests
    {
        [Fact]
        public void Class_RecognizesNameAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomName>();
            graphType.Name.ShouldBe("TestWithCustomName");
        }

        [Fact]
        public void Class_IgnoresInputNameAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomInputName>();
            graphType.Name.ShouldBe("TestClass_WithCustomInputName");
        }

        [Fact]
        public void Class_RecognizesOutputNameAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomOutputName>();
            graphType.Name.ShouldBe("TestWithCustomName");
        }

        [Fact]
        public void Class_RecognizesDescriptionAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomDescription>();
            graphType.Description.ShouldBe("Test description");
        }

        [Fact]
        public void Class_RecognizesObsoleteAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomDeprecationReason>();
            graphType.DeprecationReason.ShouldBe("Test deprecation reason");
        }

        [Fact]
        public void Class_RecognizesCustomGraphQLAttributes()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestClass_WithCustomAttributes>();
            graphType.Description.ShouldBe("Test custom description");
        }

        [Fact]
        public void Class_RecognizesMultipleAttributes()
        {
            var graphType = new AutoRegisteringObjectGraphType<TestClass_WithMultipleAttributes>();
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
        public void Field_RecognizesNameAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            graphType.Fields.Find("Test1").ShouldNotBeNull();
        }

        [Fact]
        public void Field_RecognizesDescriptionAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            var fieldType = graphType.Fields.Find("Field2").ShouldNotBeNull();
            fieldType.Description.ShouldBe("Test description");
        }

        [Fact]
        public void Field_RecognizesObsoleteAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            var fieldType = graphType.Fields.Find("Field3").ShouldNotBeNull();
            fieldType.DeprecationReason.ShouldBe("Test deprecation reason");
        }

        [Fact]
        public void Field_RecognizesCustomGraphQLAttributes()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            var fieldType = graphType.Fields.Find("Field4").ShouldNotBeNull();
            fieldType.Description.ShouldBe("Test custom description for field");
        }

        [Fact]
        public void Field_RecognizesMultipleAttributes()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            var fieldType = graphType.Fields.Find("Field5").ShouldNotBeNull();
            fieldType.Description.ShouldBe("Test description");
            fieldType.GetMetadata<string>("key1").ShouldBe("value1");
            fieldType.GetMetadata<string>("key2").ShouldBe("value2");
        }

        [Fact]
        public void Field_IgnoresInputTypeAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            var fieldType = graphType.Fields.Find("Field6").ShouldNotBeNull();
            fieldType.Type.ShouldBe(typeof(GraphQLClrOutputTypeReference<int>));
        }

        [Fact]
        public void Field_RecognizesOutputTypeAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            var fieldType = graphType.Fields.Find("Field7").ShouldNotBeNull();
            fieldType.Type.ShouldBe(typeof(IdGraphType));
        }

        [Fact]
        public void Field_IgnoresDefaultValueAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            var fieldType = graphType.Fields.Find("Field8").ShouldNotBeNull();
            fieldType.DefaultValue.ShouldBeNull();
        }

        [Fact]
        public void Field_IgnoresInputNameAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            graphType.Fields.Find("Field9").ShouldNotBeNull();
        }

        [Fact]
        public void Field_RecognizesOutputNameAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            graphType.Fields.Find("OutputField10").ShouldNotBeNull();
        }

        [Fact]
        public void Field_RecognizesIgnoreAttribute()
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            graphType.Fields.Find("Field11").ShouldBeNull();
        }

        [Theory]
        [InlineData(nameof(FieldTests.NotNullListNullableStringField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<string>>>))]
        [InlineData(nameof(FieldTests.NotNullListNotNullStringField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<string>>>>))]
        [InlineData(nameof(FieldTests.NullableListNullableStringField), typeof(ListGraphType<GraphQLClrOutputTypeReference<string>>))]
        [InlineData(nameof(FieldTests.NullableListNotNullStringField), typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<string>>>))]
        [InlineData(nameof(FieldTests.NotNullEnumerableNullableIntField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<int>>>))]
        [InlineData(nameof(FieldTests.NotNullEnumerableNotNullIntField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>>))]
        [InlineData(nameof(FieldTests.NullableEnumerableNullableIntField), typeof(ListGraphType<GraphQLClrOutputTypeReference<int>>))]
        [InlineData(nameof(FieldTests.NullableEnumerableNotNullIntField), typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
        [InlineData(nameof(FieldTests.NotNullArrayNullableTupleField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<Tuple<int, string>>>>))]
        [InlineData(nameof(FieldTests.NotNullArrayNotNullTupleField), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<Tuple<int, string>>>>>))]
        [InlineData(nameof(FieldTests.NullableArrayNullableTupleField), typeof(ListGraphType<GraphQLClrOutputTypeReference<Tuple<int, string>>>))]
        [InlineData(nameof(FieldTests.NullableArrayNotNullTupleField), typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<Tuple<int, string>>>>))]
        [InlineData(nameof(FieldTests.IdField), typeof(NonNullGraphType<IdGraphType>))]
        [InlineData(nameof(FieldTests.EnumerableField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<object>>>))]
        [InlineData(nameof(FieldTests.CollectionField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<object>>>))]
        [InlineData(nameof(FieldTests.TaskStringField), typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
        [InlineData(nameof(FieldTests.DataLoaderNullableStringField), typeof(GraphQLClrOutputTypeReference<string>))]
        [InlineData(nameof(FieldTests.NullableDataLoaderStringField), typeof(GraphQLClrOutputTypeReference<string>))]
        [InlineData(nameof(FieldTests.TaskDataLoaderStringArrayField), typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<string>>>))]
        public void Field_DectectsProperType(string fieldName, Type expectedGraphType)
        {
            var graphType = new AutoRegisteringObjectGraphType<FieldTests>();
            var fieldType = graphType.Fields.Find(fieldName).ShouldNotBeNull();
            fieldType.Type.ShouldBe(expectedGraphType);
        }

        [Fact]
        public void DefaultServiceProvider_Should_Create_AutoRegisteringGraphTypes()
        {
            var provider = new DefaultServiceProvider();
            provider.GetService(typeof(AutoRegisteringObjectGraphType<TestClass>)).ShouldNotBeNull();
        }

        [Fact]
        public void RegistersReadablePropertiesAndMethodsOnly()
        {
            var inputType = new AutoRegisteringObjectGraphType<TestClass>();
            inputType.Fields.Find("Field1").ShouldNotBeNull();
            inputType.Fields.Find("Field2").ShouldNotBeNull();
            inputType.Fields.Find("Field3").ShouldBeNull();
            inputType.Fields.Find("Field4").ShouldNotBeNull();
            inputType.Fields.Find("Field5").ShouldBeNull();
        }

        [Fact]
        public void SkipsSpecifiedProperties()
        {
            var inputType = new AutoRegisteringObjectGraphType<TestClass>(x => x.Field1);
            inputType.Fields.Find("Field1").ShouldBeNull();
            inputType.Fields.Find("Field2").ShouldNotBeNull();
            inputType.Fields.Find("Field3").ShouldBeNull();
            inputType.Fields.Find("Field4").ShouldNotBeNull();
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
            graph.Fields.Count.ShouldBe(1);
            graph.Fields.Find("Field5").ShouldNotBeNull();
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
            public IEnumerable EnumerableField { get; set; } = null!;
            public ICollection CollectionField { get; set; } = null!;
            public Task<string> TaskStringField() => null!;
            public IDataLoaderResult<string?> DataLoaderNullableStringField() => null!;
            public IDataLoaderResult<string>? NullableDataLoaderStringField() => null!;
            public Task<IDataLoaderResult<string?[]>> TaskDataLoaderStringArrayField() => null!;
        }

        private class TestChangingFieldList<T> : AutoRegisteringObjectGraphType<T>
        {
            protected override IEnumerable<FieldType> ProvideFields()
            {
                yield return CreateField(GetRegisteredMembers().First(x => x.Name == "Field1"))!;
            }
        }

        private class TestFieldSupport<T> : AutoRegisteringObjectGraphType<T>
        {
            protected override IEnumerable<MemberInfo> GetRegisteredMembers()
                => typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
        }

        private class TestChangingName<T> : AutoRegisteringObjectGraphType<T>
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
            public int Field1 { get; set; }
            public int Field2 { get; }
            public int Field3 { set { } }
            public int Field4() => 0;
            public int Field5;
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
            }
        }
    }
}
