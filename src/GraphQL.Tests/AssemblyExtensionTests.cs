using System.Reflection;
using GraphQL.Types;
using Moq;

namespace GraphQL.Tests;

public class AssemblyExtensionTests
{
    [Fact]
    public void GetClrTypeMappings()
    {
        GetClrTypeMappings_Test(
            new Type[]
            {
                typeof(MyClass1),
                typeof(MyClass1InputGraph),
                typeof(MyClass1OutputGraph),
                typeof(ConsoleColor),
                typeof(MyEnumGraph),
                typeof(DontRegisterMeGraph),
                typeof(DontRegisterDerivedGraph),
                typeof(DerivedGraph),
                typeof(MyObjectInputGraph),
                typeof(MyObjectOutputGraph),
                typeof(MyGenericGraph<string>),
            },
            new (Type ClrType, Type GraphType)[]
            {
                (typeof(MyClass1), typeof(MyClass1InputGraph)),
                (typeof(MyClass1), typeof(MyClass1OutputGraph)),
                (typeof(ConsoleColor), typeof(MyEnumGraph)),
                (typeof(MyClass1), typeof(DerivedGraph)),
                (typeof(int), typeof(MyGenericGraph<string>)), //ensure CLR type is pulled from derived class's parent ObjectGraphType<int>
            });
    }

    [Fact]
    public void GetClrTypeMappings_StarWars()
    {
        GetClrTypeMappings_Test(
            typeof(GraphQL.StarWars.StarWarsQuery).Assembly.GetTypes(),
            new (Type ClrType, Type GraphType)[]
            {
                (typeof(GraphQL.StarWars.Types.Droid), typeof(GraphQL.StarWars.Types.DroidType)),
                (typeof(GraphQL.StarWars.Types.Human), typeof(GraphQL.StarWars.Types.HumanType)),
            });
    }

    private void GetClrTypeMappings_Test(Type[] typeList, IEnumerable<(Type ClrType, Type GraphType)> expected)
    {
        var mockAssembly = new Mock<MockableAssembly>(MockBehavior.Strict);
        mockAssembly.Setup(x => x.GetTypes()).Returns(typeList).Verifiable();
        var actual = mockAssembly.Object.GetClrTypeMappings();
        actual.ShouldBe(expected);
        mockAssembly.Verify();
    }

    public class MockableAssembly : Assembly
    {
    }

    public class MyClass1
    {
    }

    public class MyClass1OutputGraph : ObjectGraphType<MyClass1>
    {
    }

    public class MyClass1InputGraph : InputObjectGraphType<MyClass1>
    {
    }

    public class MyEnumGraph : EnumerationGraphType<ConsoleColor>
    {
    }

    [DoNotMapClrType]
    public class DontRegisterMeGraph : ObjectGraphType<MyClass1>
    {
    }

    public class DerivedGraph : MyClass1OutputGraph
    {
    }

    public class DontRegisterDerivedGraph : DontRegisterMeGraph
    {
    }

    public class MyObjectOutputGraph : ObjectGraphType<object>
    {
    }

    public class MyObjectInputGraph : InputObjectGraphType<object>
    {
    }

    public class MyGenericGraph<T> : ObjectGraphType<int>
    {
    }
}
