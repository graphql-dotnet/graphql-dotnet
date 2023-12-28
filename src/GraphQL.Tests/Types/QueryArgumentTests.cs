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

    [Fact]
    public void validation_method_compounds_existing_validation()
    {
        var arg = new QueryArgument<StringGraphType>();
        arg.Validate(value =>
        {
            if (value is int)
                throw new ArgumentException();
        });
        arg.Validate(value =>
        {
            if (value is string)
                throw new InvalidOperationException();
        });
        arg.Validator(Guid.NewGuid());
        Should.Throw<ArgumentException>(() => arg.Validator(123));
        Should.Throw<InvalidOperationException>(() => arg.Validator("abc"));
    }

    [Fact]
    public void parsevalue_method_replaces_validation()
    {
        var arg = new QueryArgument<StringGraphType>();
        arg.ParseValue(value => "456");
        arg.ParseValue(value =>
        {
            if ((string)value == "123")
                return "789";
            return value;
        });
        arg.Parser("123").ShouldBeOfType<string>().ShouldBe("789");
    }
}
