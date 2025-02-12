#nullable enable

using GraphQL.Types;
using GraphQLParser.AST;

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

    [Fact]
    public void overriding_initialize_still_works()
    {
        var queryObject = new ObjectGraphType() { Name = "Query" };
        queryObject.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryObject };
        var inputType = new MyInput3Type();
        schema.RegisterType(inputType);
        schema.Initialize();
        inputType.ParseDictionary(new Dictionary<string, object?>()).ShouldBeOfType<MyInput3>();
    }

    public class MyInput3
    {
        public string? Name { get; set; }
    }

    public class MyInput3Type : InputObjectGraphType<MyInput3>
    {
        public MyInput3Type()
        {
            Field(x => x.Name);
        }

        public override void Initialize(ISchema schema) { }
    }

    [Fact]
    public async Task input_parser_works()
    {
        // demonstrates having a StringGraphType field that accepts a Uri as input
        // the string value is coerced to a Uri prior to beginning execution of the request
        var inputType = new InputObjectGraphType<Class1>();
        inputType.Field(x => x.Url, type: typeof(StringGraphType))
            .ParseValue(value => new Uri((string)value));
        var queryType = new ObjectGraphType();
        queryType.Field<StringGraphType>(
            "test",
            arguments: new QueryArguments(
                new QueryArgument(inputType)
                {
                    Name = "input"
                }),
            resolve: context =>
            {
                var input = context.GetArgument<Class1>("input");
                return input.Url?.ToString();
            });
        var schema = new Schema { Query = queryType };

        // check with valid url
        var result = await schema.ExecuteAsync(_ => _.Query = """{ test(input: { url: "http://www.google.com" }) }""");
        result.ShouldBeSimilarTo("""{"data":{"test":"http://www.google.com/"}}""");

        // check with invalid url
        result = await schema.ExecuteAsync(_ => _.Query = """{ test(input: { url: "abcd" }) }""");
        result.ShouldBeSimilarTo(
            """
                {"errors":[
                    {
                        "message":"Invalid value for argument \u0027input\u0027 of field \u0027test\u0027. Invalid URI: The format of the URI could not be determined.",
                        "locations":[{"line":1,"column":22}],
                        "extensions":{
                            "code":"INVALID_VALUE",
                            "codes":["INVALID_VALUE","URI_FORMAT"],
                            "number":"5.6"
                        }
                    }
                ]}
            """);
    }

    private class Class1
    {
        public Uri? Url { get; set; }
    }

    [Fact]
    public async Task input_validator_works()
    {
        // demonstrates having a StringGraphType field that accepts only strings of length 5
        var inputType = new InputObjectGraphType<Class2>();
        inputType.Field(x => x.String, type: typeof(StringGraphType))
            .Validate(value =>
            {
                if (((string)value).Length != 5)
                    throw new ArgumentException("String must be a length of 5 characters.");
            });
        var queryType = new ObjectGraphType();
        queryType.Field<StringGraphType>(
            "test",
            arguments: new QueryArguments(
                new QueryArgument(inputType)
                {
                    Name = "input"
                }),
            resolve: context =>
            {
                var input = context.GetArgument<Class2>("input");
                return input.String;
            });
        var schema = new Schema { Query = queryType };

        // check with valid string
        var result = await schema.ExecuteAsync(_ => _.Query = """{ test(input: { string: "abcde" }) }""");
        result.ShouldBeSimilarTo("""{"data":{"test":"abcde"}}""");

        // check with null string
        result = await schema.ExecuteAsync(_ => _.Query = """{ test(input: { string: null }) }""");
        result.ShouldBeSimilarTo("""{"data":{"test":null}}""");

        // check with invalid string
        result = await schema.ExecuteAsync(_ => _.Query = """{ test(input: { string: "abcd" }) }""");
        result.ShouldBeSimilarTo(
            """
                {"errors":[
                    {
                        "message":"Invalid value for argument \u0027input\u0027 of field \u0027test\u0027. String must be a length of 5 characters.",
                        "locations":[{"line":1,"column":25}],
                        "extensions":{
                            "code":"INVALID_VALUE",
                            "codes":["INVALID_VALUE","ARGUMENT"],
                            "number":"5.6"
                        }
                    }
                ]}
            """);
    }

    private class Class2
    {
        public string? String { get; set; }
    }

    [Fact]
    public void validation_method_compounds_existing_validation()
    {
        var inputType = new InputObjectGraphType();
        var fieldDef = inputType.Field<StringGraphType>("test")
            .Validate(value =>
            {
                if (value is int)
                    throw new ArgumentException();
            })
            .Validate(value =>
            {
                if (value is string)
                    throw new InvalidOperationException();
            })
            .FieldType;
        fieldDef.Validator.ShouldNotBeNull().Invoke(Guid.NewGuid());
        Should.Throw<ArgumentException>(() => fieldDef.Validator(123));
        Should.Throw<InvalidOperationException>(() => fieldDef.Validator("abc"));
    }

    [Fact]
    public void parsevalue_method_replaces_validation()
    {
        var inputType = new InputObjectGraphType();
        var fieldDef = inputType.Field<StringGraphType>("test")
            .ParseValue(value => "456")
            .ParseValue(value =>
            {
                if ((string)value == "123")
                    return "789";
                return value;
            })
            .FieldType;
        fieldDef.Parser.ShouldNotBeNull().Invoke("123").ShouldBeOfType<string>().ShouldBe("789");
    }

    [Fact]
    public void values_parsed_with_field_expression()
    {
        var inputType = new InputObjectGraphType<Class3>();
        inputType.Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<StringGraphType>("test")
            .Argument<int>("input", configure: arg => arg.ResolvedType = inputType);
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        // verify that during input coercion, the value is converted to an integer
        inputType.Fields.First().Parser.ShouldNotBeNull().Invoke("123").ShouldBe(123);
        // verify that during input coercion, parsing errors throw an exception
        Should.Throw<FormatException>(() => inputType.Fields.First().Parser.ShouldNotBeNull().Invoke("abc"));
    }

    private class Class3
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// CLR type with a single property that is intended to be mapped.
    /// </summary>
    public class TestInput
    {
        public string? Mapped { get; set; }
    }

    /// <summary>
    /// An input graph type for <see cref="TestInput"/> that defines two fields.
    /// The "mapped" field is bound to the CLR property, whereas the "skipped" field is
    /// marked with NoClrMapping so that its value is ignored when constructing the CLR object.
    /// </summary>
    public class TestInputGraphType : InputObjectGraphType<TestInput>
    {
        public TestInputGraphType()
        {
            Name = "TestInput";

            // This field is automatically mapped to TestInput.Mapped.
            Field("mapped", x => x.Mapped);

            // This field is defined on the graph type but is marked to skip CLR mapping.
            // Its value will be available during query execution (if needed) but will not be used
            // when constructing the CLR instance via ParseDictionary.
            Field<StringGraphType>("skipped").NoClrMapping();

            // Manually initialize the graph type.
            foreach (var field in Fields)
                field.ResolvedType = new StringGraphType();
        }
    }

    /// <summary>
    /// Verifies that when both a mapped field and a no-CLR-mapping field are present in the input,
    /// only the mapped field is used to populate the CLR object.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ParseDictionary_PopulatesMappedFieldAndSkipsNoClrMappingField(bool compiled)
    {
        // Arrange
        var inputData = new Dictionary<string, object?>
        {
            { "mapped", "mappedValue" },
            { "skipped", "skippedValue" }
        };
        var inputType = new TestInputGraphType();
        if (compiled)
            inputType.Initialize(null!);

        // Act: Parse the input dictionary into a CLR object.
        var result = inputType.ParseDictionary(inputData) as TestInput;

        // Assert
        result.ShouldNotBeNull();
        result.Mapped.ShouldBe("mappedValue");
        // There is no CLR property for "skipped", so its value is correctly ignored.
    }

    /// <summary>
    /// Verifies that IsValidDefault and ToAST skips fields marked with NoClrMapping.
    /// </summary>
    [Fact]
    public void IsValidDefaultAndToAST_SkipsNoClrMappingField()
    {
        // Arrange: create a CLR instance with the mapped property set.
        var clrObject = new TestInput
        {
            Mapped = "defaultMapped"
        };
        var inputType = new TestInputGraphType();

        // Act & Assert: Validate that the default CLR object is considered valid.
        inputType.IsValidDefault(clrObject).ShouldBeTrue();

        // Act: Convert the CLR object to an AST representation.
        var ast = inputType.ToAST(clrObject);

        // Assert: The resulting AST should be an GraphQLObjectValue containing only the "mapped" field.
        var objValue = ast.ShouldBeOfType<GraphQLObjectValue>();
        objValue.Fields.ShouldNotBeNull().Count.ShouldBe(1);

        var field = objValue.Fields.First();
        field.Name.StringValue.ShouldBe("mapped");

        // The field value should be a StringValue with the value "defaultMapped".
        var stringValue = field.Value.ShouldBeOfType<GraphQLStringValue>();
        stringValue.Value.ShouldBe("defaultMapped");
    }
}
