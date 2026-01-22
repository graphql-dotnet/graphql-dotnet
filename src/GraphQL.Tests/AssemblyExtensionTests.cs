using System.Reflection;
using GraphQL.DI;
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

    [Fact]
    public void CanGetNuGetVersion()
    {
        typeof(IGraphQLBuilder).Assembly.GetNuGetVersion().ShouldNotBeNull().ShouldEndWith("-preview");
    }

    [Fact]
    public void GetClrTypeMappings_WithClrTypeMappingAttribute()
    {
        GetClrTypeMappings_Test(
            new Type[]
            {
                typeof(MyClass1),
                typeof(MyClass2),
                typeof(GraphWithClrTypeMapping),
                typeof(GraphWithClrTypeMappingOverride),
                typeof(GraphWithClrTypeMappingNoBase),
                typeof(GraphWithClrTypeMappingInherited),
                typeof(GraphWithClrTypeMappingAndDoNotMap),
                typeof(BaseGraphWithClrTypeMapping),
            },
            new (Type ClrType, Type GraphType)[]
            {
                (typeof(MyClass2), typeof(GraphWithClrTypeMapping)),
                (typeof(MyClass2), typeof(GraphWithClrTypeMappingOverride)),
                (typeof(MyClass2), typeof(GraphWithClrTypeMappingNoBase)),
                (typeof(MyClass2), typeof(GraphWithClrTypeMappingInherited)),
                (typeof(MyClass2), typeof(BaseGraphWithClrTypeMapping)),
                // GraphWithClrTypeMappingAndDoNotMap should be skipped due to DoNotMapClrTypeAttribute
            });
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

    public class MyClass2
    {
    }

    // Test ClrTypeMappingAttribute specifying a CLR type on a graph type with a base
    [ClrTypeMapping(typeof(MyClass2))]
    public class GraphWithClrTypeMapping : ObjectGraphType<MyClass1>
    {
    }

    // Test ClrTypeMappingAttribute overriding inferred CLR type
    [ClrTypeMapping(typeof(MyClass2))]
    public class GraphWithClrTypeMappingOverride : ObjectGraphType<MyClass1>
    {
    }

    // Test ClrTypeMappingAttribute specifying a CLR type on a graph type with no generic base
    [ClrTypeMapping(typeof(MyClass2))]
    public class GraphWithClrTypeMappingNoBase : ObjectGraphType
    {
    }

    // Test inheritance of ClrTypeMappingAttribute
    public class GraphWithClrTypeMappingInherited : BaseGraphWithClrTypeMapping
    {
    }

    [ClrTypeMapping(typeof(MyClass2))]
    public class BaseGraphWithClrTypeMapping : ObjectGraphType<MyClass1>
    {
    }

    // Test DoNotMapClrTypeAttribute taking priority over ClrTypeMappingAttribute
    [DoNotMapClrType]
    [ClrTypeMapping(typeof(MyClass2))]
    public class GraphWithClrTypeMappingAndDoNotMap : ObjectGraphType<MyClass1>
    {
    }
}
