using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class AutoRegisteringInputObjectGraphTypeTests
    {
        [Fact]
        public void RegistersWritablePropertiesOnly()
        {
            var inputType = new AutoRegisteringInputObjectGraphType<TestClass>();
            inputType.Fields.Find("Field1").ShouldNotBeNull();
            inputType.Fields.Find("Field2").ShouldBeNull();
            inputType.Fields.Find("Field3").ShouldNotBeNull();
            inputType.Fields.Find("Field4").ShouldBeNull();
        }

        [Fact]
        public void SkipsSpecifiedProperties()
        {
            var inputType = new AutoRegisteringInputObjectGraphType<TestClass>(x => x.Field1);
            inputType.Fields.Find("Field1").ShouldBeNull();
            inputType.Fields.Find("Field2").ShouldBeNull();
            inputType.Fields.Find("Field3").ShouldNotBeNull();
            inputType.Fields.Find("Field4").ShouldBeNull();
        }

        private class TestClass
        {
            public int Field1 { get; set; }
            public int Field2 { get; }
            public int Field3 { set { } }
            public int Field5() => 0;
        }
    }
}
