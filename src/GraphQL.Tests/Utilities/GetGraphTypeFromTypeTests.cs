using System.Collections;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Utilities;

public class GetGraphTypeFromTypeTests
{
    [TheoryEx]
    //output mapping mode
    [InlineData(typeof(int), true, false, typeof(GraphQLClrOutputTypeReference<int>))]
    [InlineData(typeof(string), true, false, typeof(GraphQLClrOutputTypeReference<string>))]
    [InlineData(typeof(decimal), true, false, typeof(GraphQLClrOutputTypeReference<decimal>))]
    [InlineData(typeof(Guid), true, false, typeof(GraphQLClrOutputTypeReference<Guid>))]
    [InlineData(typeof(Uri), true, false, typeof(GraphQLClrOutputTypeReference<Uri>))]
    [InlineData(typeof(object), true, false, typeof(GraphQLClrOutputTypeReference<object>))]
    [InlineData(typeof(MyClass), true, false, typeof(GraphQLClrOutputTypeReference<MyClass>))]
    [InlineData(typeof(MyEnum), true, false, typeof(GraphQLClrOutputTypeReference<MyEnum>))]
    //output mapping mode - nullable structs
    [InlineData(typeof(int?), true, false, typeof(GraphQLClrOutputTypeReference<int>))]
    [InlineData(typeof(decimal?), true, false, typeof(GraphQLClrOutputTypeReference<decimal>))]
    [InlineData(typeof(Guid?), true, false, typeof(GraphQLClrOutputTypeReference<Guid>))]
    [InlineData(typeof(MyEnum?), true, false, typeof(GraphQLClrOutputTypeReference<MyEnum>))]
    //output mapping mode - not nullable types
    [InlineData(typeof(int), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
    [InlineData(typeof(string), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData(typeof(decimal), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<decimal>>))]
    [InlineData(typeof(Guid), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<Guid>>))]
    [InlineData(typeof(Uri), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<Uri>>))]
    [InlineData(typeof(object), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<object>>))]
    [InlineData(typeof(MyClass), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<MyClass>>))]
    [InlineData(typeof(MyEnum), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<MyEnum>>))]
    //output mapping mode - nullable structs
    [InlineData(typeof(int?), false, false, null)]
    [InlineData(typeof(decimal?), false, false, null)]
    [InlineData(typeof(Guid?), false, false, null)]
    [InlineData(typeof(MyEnum?), false, false, null)]
    //output mapping mode - various types of lists
    [InlineData(typeof(IEnumerable), true, false, typeof(ListGraphType<GraphQLClrOutputTypeReference<object>>))]
    [InlineData(typeof(IEnumerable<int>), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
    [InlineData(typeof(IEnumerable<string>), true, false, typeof(ListGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData(typeof(IEnumerable<decimal>), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<decimal>>>))]
    [InlineData(typeof(IEnumerable<Guid>), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<Guid>>>))]
    [InlineData(typeof(IEnumerable<Uri>), true, false, typeof(ListGraphType<GraphQLClrOutputTypeReference<Uri>>))]
    [InlineData(typeof(IEnumerable<MyClass>), true, false, typeof(ListGraphType<GraphQLClrOutputTypeReference<MyClass>>))]
    [InlineData(typeof(IEnumerable<MyEnum>), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<MyEnum>>>))]
    [InlineData(typeof(IList<int>), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
    [InlineData(typeof(ICollection<int>), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
    [InlineData(typeof(IReadOnlyCollection<int>), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
    [InlineData(typeof(List<int>), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
    [InlineData(typeof(int[]), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>))]
    [InlineData(typeof(IDictionary<int, string>), true, false, typeof(GraphQLClrOutputTypeReference<IDictionary<int, string>>))]
    [InlineData(typeof(IEnumerable<KeyValuePair<int, string>>), true, false, typeof(ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<KeyValuePair<int, string>>>>))]
    [InlineData(typeof(IEnumerable<int>), false, false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<int>>>>))]
    //output mapping mode - graph types
    [InlineData(typeof(IntGraphType), true, false, null)]
    [InlineData(typeof(NonNullGraphType<IntGraphType>), true, false, null)]
    [InlineData(typeof(ListGraphType<IntGraphType>), true, false, null)]
    [InlineData(typeof(IntGraphType), false, false, null)]
    [InlineData(typeof(NonNullGraphType<IntGraphType>), false, false, null)]
    [InlineData(typeof(ListGraphType<IntGraphType>), false, false, null)]
    //input mapping mode
    [InlineData(typeof(int), true, true, typeof(GraphQLClrInputTypeReference<int>))]
    //data loader compatibility
    [InlineData(typeof(IDataLoaderResult), true, false, typeof(GraphQLClrOutputTypeReference<object>))]
    [InlineData(typeof(IDataLoaderResult), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<object>>))]
    [InlineData(typeof(IDataLoaderResult<string>), true, false, typeof(GraphQLClrOutputTypeReference<string>))]
    [InlineData(typeof(IDataLoaderResult<string>), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>))]
    [InlineData(typeof(IDataLoaderResult<int>), true, false, typeof(GraphQLClrOutputTypeReference<int>))]
    [InlineData(typeof(IDataLoaderResult<int>), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
    [InlineData(typeof(IDataLoaderResult<int?>), true, false, typeof(GraphQLClrOutputTypeReference<int>))]
    [InlineData(typeof(IDataLoaderResult<int?>), false, false, null)]
    [InlineData(typeof(IDataLoaderResult<IDataLoaderResult<int>>), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<int>>))]
    [InlineData(typeof(IDataLoaderResult<IEnumerable<IDataLoaderResult<int>>>), false, false, typeof(NonNullGraphType<ListGraphType<GraphQLClrOutputTypeReference<int>>>))]
    //task incompatibility
    [InlineData(typeof(Task), true, false, null)]
    [InlineData(typeof(Task<string>), true, false, null)]
    [InlineData(typeof(AttributeTest1), true, true, typeof(CustomInputGraphType))]
    [InlineData(typeof(AttributeTest1), false, true, typeof(NonNullGraphType<CustomInputGraphType>))]
    [InlineData(typeof(AttributeTest2), true, true, typeof(CustomInputGraphType))]
    [InlineData(typeof(AttributeTest2), false, true, typeof(NonNullGraphType<CustomInputGraphType>))]
    [InlineData(typeof(AttributeTest3), true, true, typeof(GraphQLClrInputTypeReference<AttributeTest3>))]
    [InlineData(typeof(AttributeTest3), false, true, typeof(NonNullGraphType<GraphQLClrInputTypeReference<AttributeTest3>>))]
    [InlineData(typeof(AttributeTest1), true, false, typeof(CustomOutputGraphType))]
    [InlineData(typeof(AttributeTest1), false, false, typeof(NonNullGraphType<CustomOutputGraphType>))]
    [InlineData(typeof(AttributeTest2), true, false, typeof(GraphQLClrOutputTypeReference<AttributeTest2>))]
    [InlineData(typeof(AttributeTest2), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<AttributeTest2>>))]
    [InlineData(typeof(AttributeTest3), true, false, typeof(CustomOutputGraphType))]
    [InlineData(typeof(AttributeTest3), false, false, typeof(NonNullGraphType<CustomOutputGraphType>))]
    [InlineData(typeof(AttributeTest4), true, true, typeof(CustomInputGraphType))]
    [InlineData(typeof(AttributeTest4), false, true, typeof(NonNullGraphType<CustomInputGraphType>))]
    [InlineData(typeof(AttributeTest5), true, true, typeof(CustomInputGraphType))]
    [InlineData(typeof(AttributeTest5), false, true, typeof(NonNullGraphType<CustomInputGraphType>))]
    [InlineData(typeof(AttributeTest6), true, true, typeof(GraphQLClrInputTypeReference<AttributeTest6>))]
    [InlineData(typeof(AttributeTest6), false, true, typeof(NonNullGraphType<GraphQLClrInputTypeReference<AttributeTest6>>))]
    [InlineData(typeof(AttributeTest4), true, false, typeof(CustomOutputGraphType))]
    [InlineData(typeof(AttributeTest4), false, false, typeof(NonNullGraphType<CustomOutputGraphType>))]
    [InlineData(typeof(AttributeTest5), true, false, typeof(GraphQLClrOutputTypeReference<AttributeTest5>))]
    [InlineData(typeof(AttributeTest5), false, false, typeof(NonNullGraphType<GraphQLClrOutputTypeReference<AttributeTest5>>))]
    [InlineData(typeof(AttributeTest6), true, false, typeof(CustomOutputGraphType))]
    [InlineData(typeof(AttributeTest6), false, false, typeof(NonNullGraphType<CustomOutputGraphType>))]
    public void GetGraphTypeFromType_Matrix(Type type, bool nullable, bool isInputMode, Type expectedType)
    {
        if (expectedType == null)
        {
            Should.Throw<ArgumentOutOfRangeException>(() => type.GetGraphTypeFromType(nullable, isInputMode));
        }
        else
        {
            type.GetGraphTypeFromType(nullable, isInputMode).ShouldBe(expectedType);
        }
    }

    [Theory]
    [InlineData(typeof(IntGraphType), typeof(IntGraphType))]
    [InlineData(typeof(StringGraphType), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<StringGraphType>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<StringGraphType>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(MyClassObjectType), typeof(MyClassObjectType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassObjectType>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassObjectType>>>>))]
    [InlineData(typeof(GraphQLClrOutputTypeReference<int>), typeof(IntGraphType))]
    [InlineData(typeof(GraphQLClrOutputTypeReference<string>), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<GraphQLClrOutputTypeReference<string>>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<GraphQLClrOutputTypeReference<string>>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(GraphQLClrOutputTypeReference<MyClass>), typeof(MyClassObjectType))]
    [InlineData(typeof(GraphQLClrOutputTypeReference<MyEnum>), typeof(EnumerationGraphType<MyEnum>))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<MyClass>>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassObjectType>>>>))]
    public void OutputTypeIsDereferenced(Type referenceType, Type mappedType)
    {
        var query = new ObjectGraphType();
        query.Field("test", referenceType);
        var schema = new Schema
        {
            Query = query
        };
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassObjectType));
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassInputType));
        schema.Initialize();
        CompareResolvedType(schema.Query.Fields.Find("test")!.ResolvedType, mappedType);
    }

    [Theory]
    [InlineData(typeof(IntGraphType), typeof(IntGraphType))]
    [InlineData(typeof(StringGraphType), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<StringGraphType>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<StringGraphType>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(MyClassInputType), typeof(MyClassInputType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<int>), typeof(IntGraphType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<string>), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<GraphQLClrInputTypeReference<string>>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyClass>), typeof(MyClassInputType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyEnum>), typeof(EnumerationGraphType<MyEnum>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MappedEnum>), typeof(MappedEnumGraphType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<MyClass>>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    public void InputTypeIsDereferenced_Argument(Type referenceType, Type mappedType)
    {
        var query = new ObjectGraphType();
        query.Field("test", typeof(IntGraphType))
            .Argument(referenceType, "arg");
        var schema = new Schema
        {
            Query = query
        };
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassObjectType));
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassInputType));
        schema.RegisterTypeMapping(typeof(MappedEnum), typeof(MappedEnumGraphType));
        schema.Initialize();
        CompareResolvedType(schema.Query.Fields.Find("test")!.Arguments!.Find("arg")!.ResolvedType, mappedType);
    }

    private void CompareResolvedType(IGraphType? actual, Type expected)
    {
        actual.ShouldNotBeNull();
        if (expected.IsGenericType)
        {
            var genericExpectedType = expected.GetGenericTypeDefinition();
            if (genericExpectedType == typeof(NonNullGraphType<>))
            {
                var innerGraphType = actual.ShouldBeOfType<NonNullGraphType>().ResolvedType;
                CompareResolvedType(innerGraphType, expected.GetGenericArguments()[0]);
                return;
            }
            else if (genericExpectedType == typeof(ListGraphType<>))
            {
                var innerGraphType = actual.ShouldBeOfType<ListGraphType>().ResolvedType;
                CompareResolvedType(innerGraphType, expected.GetGenericArguments()[0]);
                return;
            }
        }
        actual.ShouldBeAssignableTo(expected);
    }

    [Theory]
    [InlineData(typeof(IntGraphType), typeof(IntGraphType))]
    [InlineData(typeof(StringGraphType), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<StringGraphType>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<StringGraphType>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(MyClassInputType), typeof(MyClassInputType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<int>), typeof(IntGraphType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<string>), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<GraphQLClrInputTypeReference<string>>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyClass>), typeof(MyClassInputType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyEnum>), typeof(EnumerationGraphType<MyEnum>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MappedEnum>), typeof(MappedEnumGraphType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<MyClass>>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    public void InputTypeIsDereferenced_DirectiveArgument(Type referenceType, Type mappedType)
    {
        var query = new ObjectGraphType();
        query.Field("test", typeof(StringGraphType));
        var schema = new Schema
        {
            Query = query
        };
        var directive = new Directive("MyDirective")
        {
            Arguments =
            [
                new QueryArgument(referenceType) { Name = "arg" }
            ]
        };
        directive.Locations.Add(DirectiveLocation.Field);
        schema.Directives.Register(directive);
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassObjectType));
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassInputType));
        schema.RegisterTypeMapping(typeof(MappedEnum), typeof(MappedEnumGraphType));
        schema.Initialize();
        CompareResolvedType(schema.Directives.Find("MyDirective")!.Arguments!.Find("arg")!.ResolvedType, mappedType);
    }

    [Theory]
    [InlineData(typeof(IntGraphType), typeof(IntGraphType))]
    [InlineData(typeof(StringGraphType), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<StringGraphType>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<StringGraphType>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(MyClassInputType), typeof(MyClassInputType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<int>), typeof(IntGraphType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<string>), typeof(StringGraphType))]
    [InlineData(typeof(ListGraphType<GraphQLClrInputTypeReference<string>>), typeof(ListGraphType<StringGraphType>))]
    [InlineData(typeof(NonNullGraphType<GraphQLClrInputTypeReference<string>>), typeof(NonNullGraphType<StringGraphType>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyClass>), typeof(MyClassInputType))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MyEnum>), typeof(EnumerationGraphType<MyEnum>))]
    [InlineData(typeof(GraphQLClrInputTypeReference<MappedEnum>), typeof(MappedEnumGraphType))]
    [InlineData(typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<GraphQLClrInputTypeReference<MyClass>>>>>), typeof(NonNullGraphType<ListGraphType<ListGraphType<NonNullGraphType<MyClassInputType>>>>))]
    public void InputTypeIsDereferenced_InputField(Type referenceType, Type mappedType)
    {
        var inputType = new InputObjectGraphType();
        inputType.Field("field", referenceType);
        var query = new ObjectGraphType();
        query.Field("test", typeof(IntGraphType))
            .Arguments(new QueryArgument(inputType) { Name = "arg" });
        var schema = new Schema
        {
            Query = query
        };
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassObjectType));
        schema.RegisterTypeMapping(typeof(MyClass), typeof(MyClassInputType));
        schema.RegisterTypeMapping(typeof(MappedEnum), typeof(MappedEnumGraphType));
        schema.Initialize();
        var inputTypeActual = schema.Query.Fields.Find("test")!.Arguments!.Find("arg")!.ResolvedType.ShouldBeOfType<InputObjectGraphType>();
        inputTypeActual.ShouldBe(inputType);
        CompareResolvedType(inputTypeActual.Fields.Find("field")!.ResolvedType, mappedType);
    }

    [Fact]
    public void GraphTypesAsClrTypesAreDisallowed()
    {
        var expectedMessage = "The graph type 'IntGraphType' cannot be used as a CLR type.";

        Should.Throw<ArgumentOutOfRangeException>(() => typeof(IntGraphType).GetGraphTypeFromType(false, false))
            .Message.ShouldStartWith(expectedMessage);

        var graphType = new ObjectGraphType();
        Should.Throw<ArgumentException>(() => graphType.Field<IntGraphType>("test", true))
            .Message.ShouldStartWith("The GraphQL type for field 'Object.test' could not be derived implicitly from type 'IntGraphType'. " + expectedMessage);

        var fieldBuilder = graphType.Field<string>("example");
        Should.Throw<ArgumentException>(() => fieldBuilder.Argument<IntGraphType>("test", true))
            .Message.ShouldStartWith("The GraphQL type for argument 'example.test' could not be derived implicitly from type 'IntGraphType'. " + expectedMessage);
    }

    private class MyClassObjectType : ObjectGraphType<MyClass>
    {
        public MyClassObjectType()
        {
            Field<IntGraphType>("field");
        }
    }

    private class MyClassInputType : InputObjectGraphType
    {
        public MyClassInputType()
        {
            Field<IntGraphType>("field");
        }
    }

    private class MyClass
    {
    }

    private enum MyEnum
    {
        Value1
    }

    private enum MappedEnum
    {
        Value1
    }

    private class MappedEnumGraphType : EnumerationGraphType<MappedEnum>
    {
    }

    private class CustomInputGraphType : InputObjectGraphType
    {
    }

    private class CustomOutputGraphType : ObjectGraphType
    {
    }

    [InputType(typeof(CustomInputGraphType)), OutputType(typeof(CustomOutputGraphType))]
    private class AttributeTest1 { }

    [InputType(typeof(CustomInputGraphType))]
    private class AttributeTest2 { }

    [OutputType(typeof(CustomOutputGraphType))]
    private class AttributeTest3 { }

#if NET48
    [InputType(typeof(CustomInputGraphType))]
    [OutputType(typeof(CustomOutputGraphType))]
#else
    [InputType<CustomInputGraphType>()]
    [OutputType<CustomOutputGraphType>()]
#endif
    private class AttributeTest4 { }
#if NET48
    [InputType(typeof(CustomInputGraphType))]
#else
    [InputType<CustomInputGraphType>()]
#endif
    private class AttributeTest5 { }
#if NET48
    [OutputType(typeof(CustomOutputGraphType))]
#else
    [OutputType<CustomOutputGraphType>()]
#endif
    private class AttributeTest6 { }
}
