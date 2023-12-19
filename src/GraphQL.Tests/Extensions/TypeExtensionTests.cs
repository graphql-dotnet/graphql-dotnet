using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.Tests.Extensions;

[Collection("StaticTests")]
public class TypeExtensionTests
{
    [Theory]

    // v8 naming
    [InlineData(false, false, typeof(Class1), "Class1")]
    [InlineData(false, false, typeof(Class1Type), "Class1")]
    [InlineData(false, false, typeof(Class1Graph), "Class1Graph")]
    [InlineData(false, false, typeof(Class1GraphType), "Class1")]
    [InlineData(false, false, typeof(GenericClass<>), "TGenericClass")]
    [InlineData(false, false, typeof(GenericClassType<>), "TGenericClass")]
    [InlineData(false, false, typeof(GenericClassGraph<>), "TGenericClassGraph")]
    [InlineData(false, false, typeof(GenericClassGraphType<>), "TGenericClass")]
    [InlineData(false, false, typeof(GenericClass<Class1>), "Class1GenericClass")]
    [InlineData(false, false, typeof(GenericClassType<Class1>), "Class1GenericClass")]
    [InlineData(false, false, typeof(GenericClassGraph<Class1>), "Class1GenericClassGraph")]
    [InlineData(false, false, typeof(GenericClassGraphType<Class1>), "Class1GenericClass")]
    [InlineData(false, false, typeof(GenericClass<Class1Type>), "Class1GenericClass")]
    [InlineData(false, false, typeof(GenericClass<Class1Graph>), "Class1GraphGenericClass")]
    [InlineData(false, false, typeof(GenericClass<Class1GraphType>), "Class1GenericClass")]
    [InlineData(false, false, typeof(GenericClass<Class1, string>), "Class1StringGenericClass")]
    [InlineData(false, false, typeof(GenericClassType<Class1, string>), "Class1StringGenericClass")]
    [InlineData(false, false, typeof(GenericClassGraph<Class1, string>), "Class1StringGenericClassGraph")]
    [InlineData(false, false, typeof(GenericClassGraphType<Class1, string>), "Class1StringGenericClass")]
    [InlineData(false, true, typeof(Class1), "TypeExtensionTests_Class1")]
    [InlineData(false, false, typeof(NonNullGraphType<StringGraphType>), "String")]
    [InlineData(false, false, typeof(ListGraphType<StringGraphType>), "String")]
    [InlineData(false, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>), "String")]
    [InlineData(false, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<InputObjectGraphType<Class1>>>>), "Class1InputObject")]
    [InlineData(false, false, typeof(ConnectionType<StringGraphType>), "StringConnection")]
    [InlineData(false, false, typeof(Connection<string>), "StringConnection")]

    // legacy naming
    [InlineData(true, false, typeof(Class1), "Class1")]
    [InlineData(true, false, typeof(Class1Type), "Class1")]
    [InlineData(true, false, typeof(Class1Graph), "Class1Graph")]
    [InlineData(true, false, typeof(Class1GraphType), "Class1")]
    [InlineData(true, false, typeof(GenericClass<>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClassType<>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClassGraph<>), "GenericClassGraph")]
    [InlineData(true, false, typeof(GenericClassGraphType<>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClass<Class1>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClassType<Class1>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClassGraph<Class1>), "GenericClassGraph")]
    [InlineData(true, false, typeof(GenericClassGraphType<Class1>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClass<Class1Type>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClass<Class1Graph>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClass<Class1GraphType>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClass<Class1, string>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClassType<Class1, string>), "GenericClass")]
    [InlineData(true, false, typeof(GenericClassGraph<Class1, string>), "GenericClassGraph")]
    [InlineData(true, false, typeof(GenericClassGraphType<Class1, string>), "GenericClass")]
    [InlineData(true, true, typeof(Class1), "Class1")]
    [InlineData(true, false, typeof(NonNullGraphType<StringGraphType>), "String")]
    [InlineData(true, false, typeof(ListGraphType<StringGraphType>), "String")]
    [InlineData(true, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>), "String")]
    [InlineData(true, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<InputObjectGraphType<Class1>>>>), "InputObject")]
    [InlineData(true, false, typeof(ConnectionType<StringGraphType>), "Connection")]
    [InlineData(true, false, typeof(Connection<string>), "Connection")]
    public void GraphQLNameTest(bool useLegacyTypeNaming, bool useDeclaringTypeNames, Type type, string expected)
    {
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
}
