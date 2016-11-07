using GraphQL.Types;
using Shouldly;
using System.Linq;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class ObjectGraphTypeTests
    {
        private ObjectGraphType type = new ObjectGraphType();

        private class TestInterface : InterfaceGraphType
        {
        }

        [Fact]
        public void can_implement_interfaces()
        {
            type.Interface<TestInterface>();
            type.Interfaces.Count().ShouldBe(1);
        }
    }
}
