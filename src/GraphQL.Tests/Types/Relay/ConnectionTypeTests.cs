using GraphQL.Types;
using GraphQL.Types.Relay;
using Should;

namespace GraphQL.Tests.Types.Relay
{
    public class ConnectionTypeTests
    {
        private ConnectionType<ObjectGraphType> type = new ConnectionType<ObjectGraphType>();

        [Test]
        public void should_derive_name()
        {
            type.Name.ShouldEqual("ObjectConnection");
        }
    }
}
