using System.Collections.Generic;
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
        public void DefaultServiceProvider_Should_Create_AutoRegisteringGraphTypes()
        {
            var provider = new DefaultServiceProvider();
            provider.GetService(typeof(AutoRegisteringObjectGraphType<Dummy>)).ShouldNotBeNull();
            provider.GetService(typeof(AutoRegisteringInputObjectGraphType<Dummy>)).ShouldNotBeNull();
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

        private class TestChangingFieldList<T> : AutoRegisteringObjectGraphType<T>
        {
            protected override IEnumerable<FieldType> ProvideFields()
            {
                yield return CreateField(GetRegisteredProperties().First(x => x.Name == "Field1"));
            }
        }

        private class TestChangingName<T> : AutoRegisteringObjectGraphType<T>
        {
            protected override FieldType CreateField(PropertyInfo propertyInfo)
            {
                var field = base.CreateField(propertyInfo);
                field.Name += "Prop";
                return field;
            }
        }

        private class Dummy
        {
            public string Name { get; set; }
        }

        private class TestClass
        {
            public int Field1 { get; set; }
            public int Field2 { get; }
            public int Field3 { set { } }
            public int Field4() => 0;
        }
    }
}
