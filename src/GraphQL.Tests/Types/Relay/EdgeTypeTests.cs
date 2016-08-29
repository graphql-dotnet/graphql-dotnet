using GraphQL.Types;
using GraphQL.Types.Relay;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types.Relay
{
    public class EdgeTypeTests
    {
        private EdgeType<ObjectGraphType> type = new EdgeType<ObjectGraphType>();

        [Fact]
        public void should_derive_name()
        {
            type.Name.ShouldBe("ObjectEdge");
        }
    }
}
