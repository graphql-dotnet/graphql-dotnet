using GraphQL.Types;

namespace GraphQL.Tests;

public class TypeExtensionTests
{
    [Theory]
    [InlineData(typeof(IdGraphType), true)]
    [InlineData(typeof(ObjectGraphType), true)]
    [InlineData(typeof(InputObjectGraphType), true)]
    [InlineData(typeof(NonNullGraphType<IdGraphType>), false)]
    [InlineData(typeof(ListGraphType<IdGraphType>), false)]
    [InlineData(typeof(NonNullGraphType), false)]
    [InlineData(typeof(ListGraphType), false)]
    [InlineData(null, false)]
    public void IsNamedGraphType(Type? type, bool expected)
    {
        type!.IsNamedType().ShouldBe(expected);
    }

    [Theory]
    [InlineData(typeof(Type), "Type")]
    [InlineData(typeof(GraphType), "GraphType")]
    [InlineData(typeof(Guid), "Guid")]
    [InlineData(typeof(ScalarGraphType), "Scalar")]
    [InlineData(typeof(NonNullGraphType<ListGraphType<IdGraphType>>), "Id")]
    [InlineData(typeof(TestType), "Test")]
    public void GraphQLName(Type type, string expected)
    {
        type.GraphQLName().ShouldBe(expected);
    }

    private class TestType
    {
    }
}
