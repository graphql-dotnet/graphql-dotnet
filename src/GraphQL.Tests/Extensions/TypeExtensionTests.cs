using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.Tests.Extensions;

[Collection("StaticTests")]
public class TypeExtensionTests
{
    [Theory]

    // v8 naming
    [InlineData(10, false, false, typeof(Class1), "Class1")]
    [InlineData(11, false, false, typeof(Class1Type), "Class1")]
    [InlineData(12, false, false, typeof(Class1Graph), "Class1Graph")]
    [InlineData(13, false, false, typeof(Class1GraphType), "Class1")]
    [InlineData(14, false, false, typeof(GenericClass<>), "TGenericClass")]
    [InlineData(15, false, false, typeof(GenericClassType<>), "TGenericClass")]
    [InlineData(16, false, false, typeof(GenericClassGraph<>), "TGenericClassGraph")]
    [InlineData(17, false, false, typeof(GenericClassGraphType<>), "TGenericClass")]
    [InlineData(18, false, false, typeof(GenericClass<Class1>), "Class1GenericClass")]
    [InlineData(19, false, false, typeof(GenericClassType<Class1>), "Class1GenericClass")]
    [InlineData(20, false, false, typeof(GenericClassGraph<Class1>), "Class1GenericClassGraph")]
    [InlineData(21, false, false, typeof(GenericClassGraphType<Class1>), "Class1GenericClass")]
    [InlineData(22, false, false, typeof(GenericClass<Class1Type>), "Class1GenericClass")]
    [InlineData(23, false, false, typeof(GenericClass<Class1Graph>), "Class1GraphGenericClass")]
    [InlineData(24, false, false, typeof(GenericClass<Class1GraphType>), "Class1GenericClass")]
    [InlineData(25, false, false, typeof(GenericClass<Class1, string>), "Class1StringGenericClass")]
    [InlineData(26, false, false, typeof(GenericClassType<Class1, string>), "Class1StringGenericClass")]
    [InlineData(27, false, false, typeof(GenericClassGraph<Class1, string>), "Class1StringGenericClassGraph")]
    [InlineData(28, false, false, typeof(GenericClassGraphType<Class1, string>), "Class1StringGenericClass")]
    [InlineData(29, false, true, typeof(Class1), "TypeExtensionTests_Class1")]
    [InlineData(30, false, true, typeof(GenericClass<Class1>), "TypeExtensionTests_Class1TypeExtensionTests_GenericClass")]
    [InlineData(31, false, false, typeof(NonNullGraphType<StringGraphType>), "String")]
    [InlineData(32, false, false, typeof(ListGraphType<StringGraphType>), "String")]
    [InlineData(33, false, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>), "String")]
    [InlineData(34, false, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<InputObjectGraphType<Class1>>>>), "Class1InputObject")]
    [InlineData(35, false, false, typeof(ConnectionType<StringGraphType>), "StringConnection")]
    [InlineData(36, false, false, typeof(Connection<string>), "StringConnection")]
    [InlineData(37, false, false, typeof(Test1<string>.Test2), "StringTest2")]
    [InlineData(38, false, false, typeof(Test1<string>.Test3<Class1>), "StringClass1Test3")]
    [InlineData(39, false, true, typeof(Test1<string>.Test2), "StringTypeExtensionTests_Test1_Test2")]
    [InlineData(40, false, true, typeof(Test1<string>.Test3<Class1>), "StringTypeExtensionTests_Class1TypeExtensionTests_Test1_Test3")]

    // legacy naming
    [InlineData(60, true, false, typeof(Class1), "Class1")]
    [InlineData(61, true, false, typeof(Class1Type), "Class1")]
    [InlineData(62, true, false, typeof(Class1Graph), "Class1Graph")]
    [InlineData(63, true, false, typeof(Class1GraphType), "Class1")]
    [InlineData(64, true, false, typeof(GenericClass<>), "GenericClass")]
    [InlineData(65, true, false, typeof(GenericClassType<>), "GenericClass")]
    [InlineData(66, true, false, typeof(GenericClassGraph<>), "GenericClassGraph")]
    [InlineData(67, true, false, typeof(GenericClassGraphType<>), "GenericClass")]
    [InlineData(68, true, false, typeof(GenericClass<Class1>), "GenericClass")]
    [InlineData(69, true, false, typeof(GenericClassType<Class1>), "GenericClass")]
    [InlineData(70, true, false, typeof(GenericClassGraph<Class1>), "GenericClassGraph")]
    [InlineData(71, true, false, typeof(GenericClassGraphType<Class1>), "GenericClass")]
    [InlineData(72, true, false, typeof(GenericClass<Class1Type>), "GenericClass")]
    [InlineData(73, true, false, typeof(GenericClass<Class1Graph>), "GenericClass")]
    [InlineData(74, true, false, typeof(GenericClass<Class1GraphType>), "GenericClass")]
    [InlineData(75, true, false, typeof(GenericClass<Class1, string>), "GenericClass")]
    [InlineData(76, true, false, typeof(GenericClassType<Class1, string>), "GenericClass")]
    [InlineData(77, true, false, typeof(GenericClassGraph<Class1, string>), "GenericClassGraph")]
    [InlineData(78, true, false, typeof(GenericClassGraphType<Class1, string>), "GenericClass")]
    [InlineData(79, true, true, typeof(Class1), "Class1")]
    [InlineData(80, true, true, typeof(GenericClass<Class1>), "GenericClass")]
    [InlineData(81, true, false, typeof(NonNullGraphType<StringGraphType>), "String")]
    [InlineData(82, true, false, typeof(ListGraphType<StringGraphType>), "String")]
    [InlineData(83, true, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>), "String")]
    [InlineData(84, true, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<InputObjectGraphType<Class1>>>>), "InputObject")]
    [InlineData(85, true, false, typeof(ConnectionType<StringGraphType>), "Connection")]
    [InlineData(86, true, false, typeof(Connection<string>), "Connection")]
    [InlineData(87, true, false, typeof(Test1<string>.Test2), "Test2")]
    [InlineData(88, true, false, typeof(Test1<string>.Test3<Class1>), "Test3")]
    [InlineData(89, true, true, typeof(Test1<string>.Test2), "Test2")]
    [InlineData(90, true, true, typeof(Test1<string>.Test3<Class1>), "Test3")]

    // note: cannot test F# anonymous class names within C# tests
    public void GraphQLNameTest(int i, bool useLegacyTypeNaming, bool useDeclaringTypeNames, Type type, string expected)
    {
        _ = i;
        var oldUseLegacyTypeNaming = GlobalSwitches.UseLegacyTypeNaming;
        var oldUseDeclaringTypeNames = GlobalSwitches.UseDeclaringTypeNames;
        GlobalSwitches.UseLegacyTypeNaming = useLegacyTypeNaming;
        GlobalSwitches.UseDeclaringTypeNames = useDeclaringTypeNames;
        try
        {
            type.GraphQLName().ShouldBe(expected);
        }
        finally
        {
            GlobalSwitches.UseLegacyTypeNaming = oldUseLegacyTypeNaming;
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
