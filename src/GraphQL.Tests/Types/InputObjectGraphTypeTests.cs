#nullable enable

using System.Runtime.CompilerServices;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParseDictionary_preserves_optional_member_presence(bool compiled)
    {
        var type = new InputObjectGraphType<OptionalMembersInput>();
        type.Field("Constructor", new StringGraphType());
        type.Field("Property", new IntGraphType());
        type.Field("Field", new StringGraphType());
        type.Field("Init", new StringGraphType());
        type.Field("Required", new IntGraphType());
        if (compiled)
            type.Initialize(null!);

        var omitted = type.ParseDictionary(new Dictionary<string, object?>()).ShouldBeOfType<OptionalMembersInput>();
        omitted.Constructor.IsSpecified.ShouldBeFalse();
        omitted.Property.IsSpecified.ShouldBeFalse();
        omitted.Field.IsSpecified.ShouldBeFalse();
        omitted.Init.IsSpecified.ShouldBeFalse();
        omitted.Required.IsSpecified.ShouldBeFalse();

        var explicitNull = type.ParseDictionary(new Dictionary<string, object?>
        {
            ["Constructor"] = null,
            ["Property"] = null,
            ["Field"] = null,
            ["Init"] = null,
            ["Required"] = null,
        }).ShouldBeOfType<OptionalMembersInput>();
        explicitNull.Constructor.Value.ShouldBeNull();
        explicitNull.Property.Value.ShouldBeNull();
        explicitNull.Field.Value.ShouldBeNull();
        explicitNull.Init.Value.ShouldBeNull();
        explicitNull.Required.Value.ShouldBeNull();

        var supplied = type.ParseDictionary(new Dictionary<string, object?>
        {
            ["Constructor"] = "constructor",
            ["Property"] = 42L,
            ["Field"] = "field",
            ["Init"] = "init",
            ["Required"] = 0,
        }).ShouldBeOfType<OptionalMembersInput>();
        supplied.Constructor.Value.ShouldBe("constructor");
        supplied.Property.Value.ShouldBe(42);
        supplied.Field.Value.ShouldBe("field");
        supplied.Init.Value.ShouldBe("init");
        supplied.Required.Value.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParseDictionary_supports_optional_lists_objects_and_nested_optionals(bool compiled)
    {
        var childType = new InputObjectGraphType<OptionalChildInput>();
        childType.Field("Name", new StringGraphType());
        var type = new InputObjectGraphType<OptionalContainersInput>();
        type.Field("Items", new ListGraphType(new IntGraphType()));
        type.Field("List", new ListGraphType(new IntGraphType()));
        type.Field("Nested", new StringGraphType());
        type.Field("Child", childType);
        if (compiled)
            type.Initialize(null!);

        var omitted = type.ParseDictionary(new Dictionary<string, object?>()).ShouldBeOfType<OptionalContainersInput>();
        omitted.Items.ShouldBeNull();
        omitted.List.IsSpecified.ShouldBeFalse();
        omitted.Nested.IsSpecified.ShouldBeFalse();
        omitted.Child.IsSpecified.ShouldBeFalse();

        var explicitNull = type.ParseDictionary(new Dictionary<string, object?>
        {
            ["List"] = null,
            ["Nested"] = null,
            ["Child"] = null,
        }).ShouldBeOfType<OptionalContainersInput>();
        explicitNull.List.Value.ShouldBeNull();
        explicitNull.Nested.Value.Value.ShouldBeNull();
        explicitNull.Child.Value.ShouldBeNull();

        var supplied = type.ParseDictionary(new Dictionary<string, object?>
        {
            ["Items"] = new object?[] { 1L, null, 0 },
            ["List"] = new object?[] { null, 2L },
            ["Nested"] = "nested",
            ["Child"] = new Dictionary<string, object?> { ["Name"] = null },
        }).ShouldBeOfType<OptionalContainersInput>();
        supplied.Items.ShouldNotBeNull().Select(x => x.Value).ShouldBe(new int?[] { 1, null, 0 });
        supplied.List.Value.ShouldNotBeNull().Select(x => x.Value).ShouldBe(new int?[] { null, 2 });
        supplied.Nested.Value.Value.ShouldBe("nested");
        supplied.Child.Value.ShouldNotBeNull().Name.Value.ShouldBeNull();

        var empty = type.ParseDictionary(new Dictionary<string, object?>
        {
            ["List"] = Array.Empty<object>(),
            ["Child"] = new Dictionary<string, object?>(),
        }).ShouldBeOfType<OptionalContainersInput>();
        empty.List.Value.ShouldNotBeNull().ShouldBeEmpty();
        empty.Child.Value.ShouldNotBeNull().Name.IsSpecified.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task optional_inputs_preserve_presence_during_execution(bool compiled, bool useVariables)
    {
        var previous = GlobalSwitches.DynamicallyCompileToObject;
        GlobalSwitches.DynamicallyCompileToObject = compiled;
        try
        {
            var inputType = new InputObjectGraphType<OptionalExecutionInput> { Name = "OptionalInput" };
            inputType.Field(x => x.Name, type: typeof(StringGraphType));
            inputType.Field(x => x.Defaulted, type: typeof(StringGraphType)).Configure(field => field.DefaultValue = "default");
            var queryType = new ObjectGraphType { Name = "Query" };
            queryType.Field<StringGraphType>("test")
                .Arguments(new QueryArguments(new QueryArgument(inputType) { Name = "input" }))
                .Resolve(context =>
                {
                    var input = context.GetArgument<OptionalExecutionInput>("input");
                    return $"{(input.Name.IsSpecified ? input.Name.Value ?? "null" : "omitted")}/{input.Defaulted.Value}";
                });
            using var schema = new Schema { Query = queryType };

            foreach (var (literal, json, expected) in new[]
            {
                ("{}", "{}", "omitted"),
                ("{ name: null }", """{"name":null}""", "null"),
                ("{ name: \"value\" }", """{"name":"value"}""", "value"),
            })
            {
                var result = await schema.ExecuteAsync(options =>
                {
                    options.ThrowOnUnhandledException = true;
                    options.Query = useVariables
                        ? "query ($input: OptionalInput!) { test(input: $input) }"
                        : "{ test(input: " + literal + ") }";
                    if (useVariables)
                        options.Variables = new Inputs(new Dictionary<string, object?> { ["input"] = json.ToInputs() });
                });
                result.ShouldBeSimilarTo($$$"""{"data":{"test":"{{{expected}}}/default"}}""");
            }
        }
        finally
        {
            GlobalSwitches.DynamicallyCompileToObject = previous;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task optional_inputs_distinguish_null_and_omission_in_inline_objects(bool compiled, bool nested)
    {
        var previous = GlobalSwitches.DynamicallyCompileToObject;
        GlobalSwitches.DynamicallyCompileToObject = compiled;
        try
        {
            var childType = new InputObjectGraphType<OptionalChildInput>();
            childType.Field(x => x.Name, type: typeof(StringGraphType));
            var containerType = new InputObjectGraphType<OptionalContainersInput>();
            containerType.Field("Child", childType);
            Optional<string?> actual = default;
            var queryType = new ObjectGraphType { Name = "Query" };
            queryType.Field<BooleanGraphType>("test")
                .Arguments(new QueryArguments(new QueryArgument(nested ? (IGraphType)containerType : childType) { Name = "input" }))
                .Resolve(context =>
                {
                    if (nested)
                    {
                        var child = context.GetArgument<OptionalContainersInput>("input").Child;
                        child.IsSpecified.ShouldBeTrue();
                        actual = child.Value.ShouldNotBeNull().Name;
                    }
                    else
                    {
                        actual = context.GetArgument<OptionalChildInput>("input").Name;
                    }
                    return actual.IsSpecified;
                });
            using var schema = new Schema { Query = queryType };

            foreach (var (operation, input, specified) in new[]
            {
                ("", "{ name: null }", true),
                ("query ($v: String = null)", "{ name: $v }", true),
                ("query ($v: String)", "{ name: $v }", false),
                ("", "{}", false),
            })
            {
                var literal = nested ? "{ child: " + input + " }" : input;
                var result = await schema.ExecuteAsync(options =>
                {
                    options.ThrowOnUnhandledException = true;
                    options.Query = operation + " { test(input: " + literal + ") }";
                });
                result.ShouldBeSimilarTo(specified ? """{"data":{"test":true}}""" : """{"data":{"test":false}}""");
                actual.IsSpecified.ShouldBe(specified);
                if (specified)
                    actual.Value.ShouldBeNull();
            }
        }
        finally
        {
            GlobalSwitches.DynamicallyCompileToObject = previous;
        }
    }

    [Theory]
    [InlineData(false, typeof(CustomOptional<>))]
    [InlineData(true, typeof(CustomOptional<>))]
    [InlineData(false, typeof(CustomOptionalClass<>))]
    [InlineData(true, typeof(CustomOptionalClass<>))]
    [InlineData(false, typeof(StrongBox<>))]
    [InlineData(true, typeof(StrongBox<>))]
    public void ParseDictionary_supports_registered_optional_members(bool compiled, Type optionalType)
    {
        if (optionalType == typeof(CustomOptional<>))
        {
            Check<CustomOptional<string?>>((optional, specified, value) =>
            {
                optional.Present.ShouldBe(specified);
                optional.Data.ShouldBe(value);
            });
        }
        else if (optionalType == typeof(CustomOptionalClass<>))
        {
            Check<CustomOptionalClass<string?>>((optional, specified, value) =>
            {
                optional.ShouldNotBeNull().Present.ShouldBe(specified);
                optional.Data.ShouldBe(value);
            });
            Check<CustomOptionalClass<object?>>((optional, specified, value) =>
            {
                optional.ShouldNotBeNull().Present.ShouldBe(specified);
                optional.Data.ShouldBe(value);
            });
        }
        else
        {
            Check<StrongBox<string?>>((optional, _, value) => optional.ShouldNotBeNull().Value.ShouldBe(value));
        }

        void Check<TOptional>(Action<TOptional, bool, string?> assert)
        {
            var type = new InputObjectGraphType<RegisteredOptionalMembersInput<TOptional>>();
            string[] members = ["Constructor", "Property", "Field", "Init", "Required"];
            foreach (var member in members)
                type.Field(member, new StringGraphType());
            if (compiled)
                type.Initialize(null!);

            foreach (var (specified, value) in new (bool, string?)[] { (false, null), (true, null), (true, "value") })
            {
                var data = specified
                    ? members.ToDictionary(member => member, _ => (object?)value)
                    : new Dictionary<string, object?>();
                var result = type.ParseDictionary(data).ShouldBeOfType<RegisteredOptionalMembersInput<TOptional>>();
                foreach (var member in new[] { result.Constructor, result.Property, result.Field, result.Init, result.Required })
                    assert(member, specified, value);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParseDictionary_supports_registered_optionals_in_lists_and_nested_optionals(bool compiled)
    {
        var type = new InputObjectGraphType<RegisteredOptionalContainersInput>();
        type.Field("StructItems", new ListGraphType(new StringGraphType()));
        type.Field("ClassItems", new ListGraphType(new StringGraphType()));
        type.Field("NestedStruct", new StringGraphType());
        type.Field("NestedClass", new StringGraphType());
        type.Field("List", new ListGraphType(new StringGraphType()));
        if (compiled)
            type.Initialize(null!);

        var omitted = type.ParseDictionary(new Dictionary<string, object?>()).ShouldBeOfType<RegisteredOptionalContainersInput>();
        omitted.NestedStruct.Present.ShouldBeFalse();
        omitted.NestedClass.ShouldNotBeNull().Present.ShouldBeFalse();
        omitted.List.ShouldNotBeNull().Present.ShouldBeFalse();

        foreach (var value in new[] { null, "value" })
        {
            var data = new Dictionary<string, object?>
            {
                ["StructItems"] = new object?[] { value },
                ["ClassItems"] = new object?[] { value },
                ["NestedStruct"] = value,
                ["NestedClass"] = value,
                ["List"] = new object?[] { value },
            };
            var result = type.ParseDictionary(data).ShouldBeOfType<RegisteredOptionalContainersInput>();
            var structItem = result.StructItems.ShouldNotBeNull().Single();
            structItem.Present.ShouldBeTrue();
            structItem.Data.ShouldBe(value);
            var classItem = result.ClassItems.ShouldNotBeNull().Single().ShouldNotBeNull();
            classItem.Present.ShouldBeTrue();
            classItem.Data.ShouldBe(value);
            result.NestedStruct.Present.ShouldBeTrue();
            result.NestedStruct.Data.ShouldNotBeNull().Present.ShouldBeTrue();
            result.NestedStruct.Data.Data.ShouldBe(value);
            result.NestedClass.ShouldNotBeNull().Present.ShouldBeTrue();
            result.NestedClass.Data.Present.ShouldBeTrue();
            result.NestedClass.Data.Data.ShouldBe(value);
            result.List.ShouldNotBeNull().Present.ShouldBeTrue();
            var listItem = result.List.Data.ShouldNotBeNull().Single();
            listItem.Present.ShouldBeTrue();
            listItem.Data.ShouldBe(value);
        }
    }

    private class RegisteredOptionalMembersInput<TOptional>
    {
        public RegisteredOptionalMembersInput(TOptional constructor) => Constructor = constructor;

        public TOptional Constructor { get; }
        public TOptional Property { get; set; }
        public TOptional Field = default!;
        public TOptional Init { get; init; }
        public required TOptional Required { get; set; }
    }

    private class RegisteredOptionalContainersInput
    {
        public List<CustomOptional<string?>>? StructItems { get; set; }
        public List<CustomOptionalClass<string?>>? ClassItems { get; set; }
        public CustomOptional<CustomOptionalClass<string?>> NestedStruct { get; set; }
        public CustomOptionalClass<CustomOptional<string?>> NestedClass { get; set; }
        public CustomOptionalClass<List<CustomOptional<string?>>> List { get; set; }
    }

    private class OptionalMembersInput
    {
        public OptionalMembersInput(Optional<string?> constructor) => Constructor = constructor;

        public Optional<string?> Constructor { get; }
        public Optional<int?> Property { get; set; }
        public Optional<string?> Field = default;
        public Optional<string?> Init { get; init; }
        public required Optional<int?> Required { get; set; }
    }

    private class OptionalContainersInput
    {
        public List<Optional<int?>>? Items { get; set; }
        public Optional<List<Optional<int?>>?> List { get; set; }
        public Optional<Optional<string?>> Nested { get; set; }
        public Optional<OptionalChildInput?> Child { get; set; }
    }

    private class OptionalChildInput
    {
        public Optional<string?> Name { get; set; }
    }

    private class OptionalExecutionInput : OptionalChildInput
    {
        public Optional<string> Defaulted { get; set; }
    }
}
