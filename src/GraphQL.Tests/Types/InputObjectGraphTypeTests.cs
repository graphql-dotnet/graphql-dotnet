#nullable enable

using GraphQL.Types;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class InputObjectGraphTypeTests
{
    [Fact]
    public void should_throw_an_exception_if_input_object_graph_type_contains_object_graph_type_field()
    {
        var type = new InputObjectGraphType();
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<ObjectGraphType>("test"));

        exception.Message.ShouldContain("Input type 'InputObject' can have fields only of input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType.");
    }

    [Fact]
    public void should_throw_an_exception_if_object_graph_type_contains_Input_object_graph_type_field()
    {
        var type = new ObjectGraphType();
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<InputObjectGraphType>("test"));
        exception.Message.ShouldContain("Output type 'Object' can have fields only of output types: ScalarGraphType, ObjectGraphType, InterfaceGraphType, UnionGraphType or EnumerationGraphType.");
    }

    [Fact]
    public void invalid_type_throws()
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryObject };
        var inputType = new MyInputType();
        schema.RegisterType(inputType);

        Should.Throw<InvalidOperationException>(() => schema.Initialize())
            .Message.ShouldBe("No public constructors found on CLR type 'MyInput'.");
    }

    [Fact]
    public void registered_value_converter_skips_validation()
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryObject };
        var inputType = new MyInputType();
        schema.RegisterType(inputType);
        ValueConverter.Register<MyInput>(_ => new MyInput2());
        try
        {
            schema.Initialize();
            inputType.ParseDictionary(new Dictionary<string, object?>()).ShouldBeOfType<MyInput2>();
        }
        finally
        {
            ValueConverter.Register<MyInput>(null);
        }
    }

    [Fact]
    public void overriding_parsedictionary_ignores_type_validation()
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryObject };
        var inputType = new MyInputCustomParseDictionaryType();
        schema.RegisterType(inputType);
        schema.Initialize();
        inputType.ParseDictionary(new Dictionary<string, object?>()).ShouldBeOfType<MyInput2>();
    }

    public abstract class MyInput
    {
        public string? Name { get; set; }
    }

    public class MyInput2 : MyInput
    {
    }

    public class MyInputType : InputObjectGraphType<MyInput>
    {
        public MyInputType()
        {
            Field(x => x.Name);
        }
    }

    public class MyInputCustomParseDictionaryType : InputObjectGraphType<MyInput>
    {
        public MyInputCustomParseDictionaryType()
        {
            Field(x => x.Name);
        }

        public override object ParseDictionary(IDictionary<string, object?> value) => new MyInput2();
    }
}
