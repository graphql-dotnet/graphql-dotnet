using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.Tests.Extensions;

[Collection("StaticTests")]
public class TypeExtensionTests
{
    [Theory]
    [InlineData(10, false, typeof(Class1), "Class1")]
    [InlineData(11, false, typeof(Class1Type), "Class1")]
    [InlineData(12, false, typeof(Class1Graph), "Class1Graph")]
    [InlineData(13, false, typeof(Class1GraphType), "Class1")]
    [InlineData(14, false, typeof(GenericClass<>), "TGenericClass")]
    [InlineData(15, false, typeof(GenericClassType<>), "TGenericClass")]
    [InlineData(16, false, typeof(GenericClassGraph<>), "TGenericClassGraph")]
    [InlineData(17, false, typeof(GenericClassGraphType<>), "TGenericClass")]
    [InlineData(18, false, typeof(GenericClass<Class1>), "Class1GenericClass")]
    [InlineData(19, false, typeof(GenericClassType<Class1>), "Class1GenericClass")]
    [InlineData(20, false, typeof(GenericClassGraph<Class1>), "Class1GenericClassGraph")]
    [InlineData(21, false, typeof(GenericClassGraphType<Class1>), "Class1GenericClass")]
    [InlineData(22, false, typeof(GenericClass<Class1Type>), "Class1GenericClass")]
    [InlineData(23, false, typeof(GenericClass<Class1Graph>), "Class1GraphGenericClass")]
    [InlineData(24, false, typeof(GenericClass<Class1GraphType>), "Class1GenericClass")]
    [InlineData(25, false, typeof(GenericClass<Class1, string>), "Class1StringGenericClass")]
    [InlineData(26, false, typeof(GenericClassType<Class1, string>), "Class1StringGenericClass")]
    [InlineData(27, false, typeof(GenericClassGraph<Class1, string>), "Class1StringGenericClassGraph")]
    [InlineData(28, false, typeof(GenericClassGraphType<Class1, string>), "Class1StringGenericClass")]
    [InlineData(29, true, typeof(Class1), "TypeExtensionTests_Class1")]
    [InlineData(30, true, typeof(GenericClass<Class1>), "TypeExtensionTests_Class1TypeExtensionTests_GenericClass")]
    [InlineData(31, false, typeof(NonNullGraphType<StringGraphType>), "String")]
    [InlineData(32, false, typeof(ListGraphType<StringGraphType>), "String")]
    [InlineData(33, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>), "String")]
    [InlineData(34, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<InputObjectGraphType<Class1>>>>), "Class1InputObject")]
    [InlineData(35, false, typeof(ConnectionType<StringGraphType>), "StringConnection")]
    [InlineData(36, false, typeof(Connection<string>), "StringConnection")]
    [InlineData(37, false, typeof(Test1<string>.Test2), "StringTest2")]
    [InlineData(38, false, typeof(Test1<string>.Test3<Class1>), "StringClass1Test3")]
    [InlineData(39, true, typeof(Test1<string>.Test2), "StringTypeExtensionTests_Test1_Test2")]
    [InlineData(40, true, typeof(Test1<string>.Test3<Class1>), "StringTypeExtensionTests_Class1TypeExtensionTests_Test1_Test3")]
    // note: cannot test F# anonymous class names within C# tests
    public void GraphQLNameTest(int i, bool useDeclaringTypeNames, Type type, string expected)
    {
        _ = i;
        var oldUseDeclaringTypeNames = GlobalSwitches.UseDeclaringTypeNames;
        GlobalSwitches.UseDeclaringTypeNames = useDeclaringTypeNames;
        try
        {
            type.GraphQLName().ShouldBe(expected);
        }
        finally
        {
            GlobalSwitches.UseDeclaringTypeNames = oldUseDeclaringTypeNames;
        }
    }

    private class Class1 { }
    private class Class1Type { }
    private class Class1Graph { }
    private class Class1GraphType { }
    private class GenericClass<T> { }
    private class GenericClassType<T> { }
    private class GenericClassGraph<T> { }
    private class GenericClassGraphType<T> { }
    private class GenericClass<T1, T2> { }
    private class GenericClassType<T1, T2> { }
    private class GenericClassGraph<T1, T2> { }
    private class GenericClassGraphType<T1, T2> { }

    private class Test1<T>
    {
        public class Test2 { }
        public class Test3<T2> { }
    }
}
