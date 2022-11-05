using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug1889WithNullability
{
    [Fact]
    public void Schema_With_Invalid_Nullability_Should_Throw()
    {
        Should.Throw<ArgumentException>(() => new Schema { Query = new Impl1() }.Initialize());
    }

    [Fact]
    public void Schema_With_Valid_Nullability_Should_Work()
    {
        new Schema { Query = new Impl2() }.Initialize();
    }
}

public class Interface1 : InterfaceGraphType
{
    public Interface1()
    {
        Field<NonNullGraphType<StringGraphType>>("a");
    }
}

public class Impl1 : ObjectGraphType
{
    public Impl1()
    {
        IsTypeOf = _ => true;
        Interface<Interface1>();

        Field<StringGraphType>("a").Resolve(_ => "");
    }
}

public class Interface2 : InterfaceGraphType
{
    public Interface2()
    {
        Field<StringGraphType>("a");
    }
}

public class Impl2 : ObjectGraphType
{
    public Impl2()
    {
        IsTypeOf = _ => true;
        Interface<Interface2>();

        Field<NonNullGraphType<StringGraphType>>("a").Resolve(_ => "");
    }
}
