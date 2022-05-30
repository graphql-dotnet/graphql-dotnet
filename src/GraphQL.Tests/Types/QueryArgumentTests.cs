using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class QueryArgumentTests
{
    [Fact]
    public void throws_exception_with_null_instance_type()
    {
        IGraphType type = null;
        Should.Throw<ArgumentOutOfRangeException>(() => new QueryArgument(type));
    }

    [Fact]
    public void throws_exception_with_null_type()
    {
        Type type = null;
        Should.Throw<ArgumentOutOfRangeException>(() => new QueryArgument(type));
    }

    [Fact]
    public void throws_exception_with_invalid_type()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new QueryArgument(typeof(string)));
    }

    [Fact]
    public void does_not_throw_with_valid_type()
    {
        new QueryArgument<StringGraphType>();
        new QueryArgument<InputObjectGraphType>();
        new QueryArgument(typeof(StringGraphType));
        new QueryArgument(typeof(InputObjectGraphType));
    }

    [Fact]
    public void does_not_throw_when_set_null()
    {
        new QueryArgument<StringGraphType>
        {
            ResolvedType = null
        };
    }

    [Fact]
    public void throw_with_object_type()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new QueryArgument(typeof(ObjectGraphType)));
        Should.Throw<ArgumentOutOfRangeException>(() => new QueryArgument<ObjectGraphType<int>>());
    }
}
