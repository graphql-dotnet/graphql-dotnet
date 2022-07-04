using GraphQL.Types;
using GraphQL.Types.Relay;

namespace GraphQL.Tests.Types.Relay;

public class EdgeTypeTests
{
    [Fact]
    public void should_derive_name()
    {
        var type = new EdgeType<ObjectGraphType>();

        type.Name.ShouldBe("ObjectEdge");
    }

    [Fact]
    public void should_derive_name_for_non_null()
    {
        var type = new EdgeType<NonNullGraphType<ObjectGraphType>>();

        type.Name.ShouldBe("ObjectEdge");
    }
}
