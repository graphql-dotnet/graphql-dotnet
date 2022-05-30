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
    public void IsNamedGraphType(Type type, bool expected)
    {
        type.IsNamedType().ShouldBe(expected);
    }
}
