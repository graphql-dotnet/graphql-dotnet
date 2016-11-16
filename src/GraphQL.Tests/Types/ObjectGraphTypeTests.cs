using GraphQL.Types;
using Shouldly;
using System.Linq;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class ObjectGraphTypeTests
    {
        private class TestInterface : InterfaceGraphType { }

        [Fact]
        public void can_implement_interfaces()
        {
            var type = new ObjectGraphType();
            type.Interface(typeof(TestInterface));
            type.Interfaces.Count().ShouldBe(1);
        }

        private class TestPoco { }

        [Fact]
        public void can_implement_interfaces_in_derived_generic()
        {
            var type = new ObjectGraphType<TestPoco>();
            type.Interface(typeof(TestInterface));
            type.Interfaces.Count().ShouldBe(1);
        }
    }
}
