using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class ObjectGraphTypeTests
    {
        private class TestInterface : InterfaceGraphType { }

        [GraphQLMetadata(Name = ":::")]
        private class TypeWithInvalidName : ObjectGraphType { }

        [Fact]
        public void can_implement_interfaces()
        {
            var type = new ObjectGraphType();
            type.Interface(typeof(TestInterface));
            type.Interfaces.Count.ShouldBe(1);
        }

        private class TestPoco { }

        [Fact]
        public void can_implement_interfaces_in_derived_generic()
        {
            var type = new ObjectGraphType<TestPoco>();
            type.Interface(typeof(TestInterface));
            type.Interfaces.Count.ShouldBe(1);
        }

        [Fact]
        public void should_throw_on_invalid_graphtype_name()
        {
            var ex = new ArgumentOutOfRangeException("name", "A type name must match /^[_a-zA-Z][_a-zA-Z0-9]*$/ but ':::' does not.");
            Should.Throw<ArgumentOutOfRangeException>(() => new TypeWithInvalidName())
                .Message.ShouldBe(ex.Message);
        }
    }
}
