#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
        public void DefaultServiceProvider_Should_Create_AutoRegisteringGraphTypes()
        {
            var provider = new DefaultServiceProvider();
            provider.GetService(typeof(AutoRegisteringObjectGraphType<TestClass>)).ShouldNotBeNull();
        }

        [Fact]
        public void RegistersReadablePropertiesOnly()
        {
            var inputType = new AutoRegisteringObjectGraphType<TestClass>();
            inputType.Fields.Find("Field1").ShouldNotBeNull();
            inputType.Fields.Find("Field2").ShouldNotBeNull();
            inputType.Fields.Find("Field3").ShouldBeNull();
            inputType.Fields.Find("Field4").ShouldBeNull();
        }

        [Fact]
        public void SkipsSpecifiedProperties()
        {
            var inputType = new AutoRegisteringObjectGraphType<TestClass>(x => x.Field1);
            inputType.Fields.Find("Field1").ShouldBeNull();
            inputType.Fields.Find("Field2").ShouldNotBeNull();
            inputType.Fields.Find("Field3").ShouldBeNull();
            inputType.Fields.Find("Field4").ShouldBeNull();
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
        }

        private class TestChangingFieldList<T> : AutoRegisteringObjectGraphType<T>
        {
            protected override IEnumerable<FieldType> ProvideFields()
            {
                yield return CreateField(GetRegisteredProperties().First(x => x.Name == "Field1"))!;
            }
        }

        private class TestChangingName<T> : AutoRegisteringObjectGraphType<T>
        {
            protected override FieldType CreateField(PropertyInfo propertyInfo)
            {
                var field = base.CreateField(propertyInfo)!;
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
