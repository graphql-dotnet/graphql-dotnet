using GraphQL.Types;
using GraphQL.Types.Relay;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types.Relay
{
    public class ConnectionTypeTests
    {
        private ConnectionType<ObjectGraphType> type = new ConnectionType<ObjectGraphType>();

        [Fact]
        public void should_derive_name()
        {
            type.Name.ShouldBe("ObjectConnection");
        }
    }
}
