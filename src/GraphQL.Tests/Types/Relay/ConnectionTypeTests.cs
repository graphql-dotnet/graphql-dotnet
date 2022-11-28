using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.Tests.Types.Relay;

public class ConnectionTypeTests
{
    [Fact]
    public void should_derive_name()
    {
        var type = new ConnectionType<ObjectGraphType>();

        type.Name.ShouldBe("ObjectConnection");
    }

    [Fact]
    public void should_derive_name_for_non_null()
    {
        var type = new ConnectionType<NonNullGraphType<ObjectGraphType>>();

        type.Name.ShouldBe("ObjectConnection");
    }
}
