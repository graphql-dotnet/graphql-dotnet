using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Execution;

public class TestComplexScalarType : ScalarGraphType
{
    public TestComplexScalarType()
    {
        Name = "ComplexScalar";
    }

    public override object? Serialize(object? value)
    {
        if (value is string && value.Equals("DeserializedValue"))
        {
            return "SerializedValue";
        }

        return value;
    }

    public override object? ParseValue(object? value)
    {
        if (value is string && value.Equals("SerializedValue"))
        {
            return "DeserializedValue";
        }

        return null;
    }

    public override object? ParseLiteral(GraphQLValue value)
    {
        if (value is GraphQLStringValue stringValue)
        {
            if (stringValue.Value.Equals("SerializedValue"))
            {
                return "DeserializedValue";
            }
        }

        return null;
    }
}

public class TestJsonScalarReturningObject : ScalarGraphType
{
    public TestJsonScalarReturningObject()
    {
        Name = "JsonScalarReturningObject";
    }

    public override object? Serialize(object? value) => value;

    public override object? ParseValue(object? value)
        => value is string stringValue ? JsonSerializer.Deserialize<TestJsonScalarObject>(stringValue) : null;

    public override object? ParseLiteral(GraphQLValue value)
        => value is GraphQLStringValue stringValue ? JsonSerializer.Deserialize<TestJsonScalarObject>((string)stringValue.Value) : null; // string conversion for NET48
}

public class TestJsonScalarObject
{
    [JsonPropertyName("stringProperty")]
    public string StringProperty { get; set; }

    [JsonPropertyName("arrayProperty")]
    public string[] ArrayProperty { get; set; }
}

public class TestInputObject : InputObjectGraphType
{
    public TestInputObject()
    {
        Name = "TestInputObject";
        Field<StringGraphType>("a");
        Field<ListGraphType<StringGraphType>>("b");
        Field<NonNullGraphType<StringGraphType>>("c");
        Field<TestComplexScalarType>("d");
    }
}

public class TestType : ObjectGraphType
{
    public TestType()
    {
        Name = "TestType";

        Field<StringGraphType>("fieldWithObjectInput")
            .Argument<TestInputObject>("input")
            .Resolve(context =>
            {
                string result = JsonSerializer.Serialize(context.GetArgument<object?>("input"));
                return result;
            });

        Field<StringGraphType>("fieldWithNullableStringInput")
            .Argument<StringGraphType>("input")
            .Resolve(context =>
            {
                object val = context.GetArgument<object>("input");
                string result = JsonSerializer.Serialize(val);
                return result;
            });

        Field<IntGraphType>("fieldWithNullableIntInput")
            .Argument<IntGraphType>("input")
            .Resolve(context => context.GetArgument<int>("input"));

        Field<StringGraphType>("fieldWithNonNullableStringInput")
            .Argument<NonNullGraphType<StringGraphType>>("input")
            .Resolve(context =>
            {
                string result = JsonSerializer.Serialize(context.GetArgument<object?>("input"));
                return result;
            });

        Field<StringGraphType>("fieldWithDefaultArgumentValue")
            .Argument<StringGraphType>("input", arg => arg.DefaultValue = "Hello World")
            .Resolve(context =>
            {
                string result = JsonSerializer.Serialize(context.GetArgument<object?>("input"));
                return result;
            });

        Field<StringGraphType>("fieldWithCustomScalarInput")
            .Argument<TestJsonScalarReturningObject>("input")
            .Resolve(context =>
            {
                var val = context.GetArgument<TestJsonScalarObject>("input");
                string stringProperty = val.StringProperty;
                string arrayProperty = string.Join(", ", val.ArrayProperty);
                return $"{stringProperty}-{arrayProperty}";
            });
    }
}

public class VariablesSchema : Schema
{
    public VariablesSchema()
    {
        Query = new TestType();
    }
}

public class Variables_With_Inline_Structs_Tests : QueryTestBase<VariablesSchema>
{
    [Fact]
    public void executes_with_complex_input()
    {
        const string query = """
            {
              fieldWithObjectInput(input: {a: "foo", b: ["bar"], c: "baz"})
            }
            """;
        const string expected = """{ "fieldWithObjectInput": "{\"a\":\"foo\",\"b\":[\"bar\"],\"c\":\"baz\"}" }""";

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void properly_parses_single_value_to_list()
    {
        const string query = """
            {
              fieldWithObjectInput(input: {a: "foo", b: "bar", c: "baz"})
            }
            """;
        const string expected = """{ "fieldWithObjectInput": "{\"a\":\"foo\",\"b\":[\"bar\"],\"c\":\"baz\"}" }""";

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void fail_on_incorrect_value()
    {
        const string query = """
            {
              fieldWithObjectInput(input: ["foo", "bar", "baz"])
            }
            """;

        var result = AssertQueryWithErrors(query, null, rules: Enumerable.Empty<IValidationRule>(), expectedErrorCount: 1, executed: false);
        result.Errors![0].Message.ShouldBe("""Invalid literal for argument 'input' of field 'fieldWithObjectInput'. Expected object value for 'TestInputObject', found not an object '["foo", "bar", "baz"]'.""");
    }
}

public class UsingVariablesTests : QueryTestBase<VariablesSchema>
{
    private const string _query = """
        query q($input: TestInputObject) {
          fieldWithObjectInput(input: $input)
        }
        """;

    [Fact]
    public void executes_with_complex_input()
    {
        const string expected = """{ "fieldWithObjectInput": "{\"a\":\"foo\",\"b\":[\"bar\"],\"c\":\"baz\"}" }""";

        var inputs = """{ "input": { "a": "foo", "b": ["bar"], "c": "baz" } }""".ToInputs();

        AssertQuerySuccess(_query, expected, inputs);
    }

    [Fact]
    public void properly_parses_single_value_to_list()
    {
        const string expected = """{ "fieldWithObjectInput": "{\"a\":\"foo\",\"b\":[\"bar\"],\"c\":\"baz\"}" }""";

        var inputs = """{ "input": { "a": "foo", "b": "bar", "c": "baz" } }""".ToInputs();

        AssertQuerySuccess(_query, expected, inputs);
    }

    [Fact]
    public void properly_parses_multiple_values_to_list()
    {
        const string expected = """{ "fieldWithObjectInput": "{\"a\":\"foo\",\"b\":[\"bar\",\"qux\"],\"c\":\"baz\"}" }""";

        var inputs = """{ "input": { "a": "foo", "b": ["bar", "qux"], "c": "baz" } }""".ToInputs();

        AssertQuerySuccess(_query, expected, inputs);
    }

    [Fact]
    public void uses_default_value_when_not_provided()
    {
        const string queryWithDefaults = """
            query q($input: TestInputObject = {a: "foo", b: ["bar"] c: "baz"}) {
              fieldWithObjectInput(input: $input)
            }
            """;

        const string expected = """{ "fieldWithObjectInput": "{\"a\":\"foo\",\"b\":[\"bar\"],\"c\":\"baz\"}" }""";

        AssertQuerySuccess(queryWithDefaults, expected);
    }

    [Fact]
    public void executes_with_complex_scalar_input()
    {
        const string expected = """{ "fieldWithObjectInput": "{\"c\":\"foo\",\"d\":\"DeserializedValue\"}" }""";

        var inputs = """{ "input": { "c": "foo", "d": "SerializedValue" } }""".ToInputs();

        AssertQuerySuccess(_query, expected, inputs);
    }

    [Fact]
    public void errors_on_null_for_nested_non_null()
    {
        const string? expected = null;

        var inputs = """{ "input": { "a": "foo", "b": "bar", "c": null } }""".ToInputs();

        var result = AssertQueryWithErrors(_query, expected, inputs, expectedErrorCount: 1, executed: false);

        var caughtError = result.Errors.ShouldHaveSingleItem();
        caughtError.ShouldNotBeNull();
        caughtError.Message.ShouldBe("Variable '$input.c' is invalid. Received a null input for a non-null variable.");
    }

    [Fact]
    public void errors_on_incorrect_type()
    {
        const string? expected = null;

        var inputs = """{ "input": "foo bar" }""".ToInputs();

        var result = AssertQueryWithErrors(_query, expected, inputs, expectedErrorCount: 1, executed: false);

        var caughtError = result.Errors.ShouldHaveSingleItem();

        caughtError.ShouldNotBeNull();
        caughtError.Message.ShouldBe(
            "Variable '$input' is invalid. Unable to parse input as a 'TestInputObject' type. Did you provide a List or Scalar value accidentally?");
    }

    [Fact]
    public void errors_on_omission_of_nested_non_null()
    {
        const string? expected = null;

        var inputs = """{ "input": { "a": "foo", "b": "bar" } }""".ToInputs();

        var result = AssertQueryWithErrors(_query, expected, inputs, expectedErrorCount: 1, executed: false);

        var caughtError = result.Errors.ShouldHaveSingleItem();
        caughtError.ShouldNotBeNull();
        caughtError.Message.ShouldBe("Variable '$input.c' is invalid. No value provided for a non-null variable.");
    }

    [Fact]
    public void errors_on_addition_of_unknown_input_field()
    {
        const string? expected = null;

        var inputs = """{ "input": { "a": "foo", "b": "bar", "c": "baz", "e": "dog" } }""".ToInputs();

        var result = AssertQueryWithErrors(_query, expected, inputs, expectedErrorCount: 1, executed: false);

        var caughtError = result.Errors.ShouldHaveSingleItem();
        caughtError.ShouldNotBeNull();
        caughtError.Message.ShouldBe("Variable '$input' is invalid. Unrecognized input fields 'e' for type 'TestInputObject'.");
    }

    [Fact]
    public void executes_with_injected_input_variables()
    {
        const string query = """
            query q($argB: [String!]!, $argC: String!, $argD: ComplexScalar) {
              fieldWithObjectInput(input: { b: $argB, c: $argC, d: $argD,  })
            }
            """;

        const string expected = """{ "fieldWithObjectInput": "{\"b\":[\"bar\",\"qux\"],\"c\":\"foo\",\"d\":\"DeserializedValue\"}" }""";

        var inputs = """{ "argB": ["bar", "qux"], "argC": "foo", "argD": "SerializedValue" }""".ToInputs();

        AssertQuerySuccess(query, expected, inputs);
    }
}

public class HandlesNullableScalarsTests : QueryTestBase<VariablesSchema>
{
    [Fact]
    public void allows_nullable_int_input_to_be_omitted()
    {
        const string query = """
        {
          fieldWithNullableIntInput
        }
        """;

        const string expected = """
        {
          "fieldWithNullableIntInput": 0
        }
        """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void allows_nullable_inputs_to_be_omitted()
    {
        const string query = """
            {
              fieldWithNullableStringInput
            }
            """;

        const string expected = """
            {
              "fieldWithNullableStringInput": "null"
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void allows_nullable_inputs_to_be_omitted_in_a_variable()
    {
        const string query = """
            query SetsNullable($value: String) {
              fieldWithNullableStringInput(input: $value)
            }
            """;

        const string expected = """
            {
              "fieldWithNullableStringInput": "null"
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void allows_nullable_inputs_to_be_omitted_in_an_unlisted_variable()
    {
        const string query = """
            query SetsNullable {
              fieldWithNullableStringInput(input: $value)
            }
            """;

        const string expected = """
            {
              "fieldWithNullableStringInput": "null"
            }
            """;

        AssertQuerySuccess(query, expected, rules: Enumerable.Empty<IValidationRule>());
    }

    [Fact]
    public void allows_nullable_inputs_to_be_set_to_null_in_a_variable()
    {
        const string query = """
            query SetsNullable($value: String) {
              fieldWithNullableStringInput(input: $value)
            }
            """;

        const string expected = """
            {
              "fieldWithNullableStringInput": "null"
            }
            """;

        var inputs = """{"value": null}""".ToInputs();

        AssertQuerySuccess(query, expected, inputs);
    }

    [Fact]
    public void allows_nullable_inputs_to_be_set_to_a_value_in_a_variable()
    {
        const string query = """
            query SetsNullable($value: String) {
              fieldWithNullableStringInput(input: $value)
            }
            """;

        // value is: "a"
        const string expected = """
            {
              "fieldWithNullableStringInput": "\"a\""
            }
            """;

        var inputs = """{"value": "a"}""".ToInputs();

        AssertQuerySuccess(query, expected, inputs);
    }

    [Fact]
    public void allows_nullable_inputs_to_be_set_to_a_value_directly()
    {
        const string query = """
            {
              fieldWithNullableStringInput(input: "a")
            }
            """;

        // value is: "a"
        const string expected = """
            {
              "fieldWithNullableStringInput": "\"a\""
            }
            """;

        AssertQuerySuccess(query, expected);
    }
}

public class HandlesNonNullScalarTests : QueryTestBase<VariablesSchema>
{
    [Fact]
    public void does_not_allow_non_nullable_inputs_to_be_omitted_in_a_variable()
    {
        const string query = """
            query SetsNonNullable($value: String!) {
              fieldWithNonNullableStringInput(input: $value)
            }
            """;

        const string? expected = null;

        var result = AssertQueryWithErrors(query, expected, expectedErrorCount: 1, executed: false);

        var caughtError = result.Errors.ShouldHaveSingleItem();
        caughtError.ShouldNotBeNull();
        caughtError.Message.ShouldBe("Variable '$value' is invalid. No value provided for a non-null variable.");
    }

    [Fact]
    public void does_not_allow_non_nullable_inputs_to_be_set_to_null_in_a_variable()
    {
        const string query = """
            query SetsNonNullable($value: String!) {
              fieldWithNonNullableStringInput(input: $value)
            }
            """;

        const string? expected = null;

        var inputs = """{"value": null}""".ToInputs();

        var result = AssertQueryWithErrors(query, expected, inputs, expectedErrorCount: 1, executed: false);

        var caughtError = result.Errors.ShouldHaveSingleItem();
        caughtError.ShouldNotBeNull();
        caughtError.Message.ShouldBe("Variable '$value' is invalid. Received a null input for a non-null variable.");
    }

    [Fact]
    public void allows_non_nullable_inputs_to_be_set_to_a_value_in_a_variable()
    {
        const string query = """
            query SetsNullable($value: String) {
              fieldWithNullableStringInput(input: $value)
            }
            """;

        const string expected = """
            {
              "fieldWithNullableStringInput": "\"a\""
            }
            """;

        var inputs = """{"value": "a"}""".ToInputs();

        AssertQuerySuccess(query, expected, inputs);
    }

    [Fact]
    public void allows_non_nullable_inputs_to_be_set_to_a_value_directly()
    {
        const string query = """
            {
              fieldWithNullableStringInput(input: "a")
            }
            """;

        const string expected = """
            {
              "fieldWithNullableStringInput": "\"a\""
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void allows_custom_scalar_that_resolves_to_an_object()
    {
        const string query = """
            query SetsCustomScalarInput($input: JsonScalarReturningObject) {
              fieldWithCustomScalarInput(input: $input)
            }
            """;

        const string expected = """
            {
              "fieldWithCustomScalarInput": "bear-cat, dog, bird"
            }
            """;

        const string jsonInput = """{ "stringProperty": "bear", "arrayProperty": ["cat", "dog", "bird"] }""";
        var jsonInputEncoded = JsonEncodedText.Encode(jsonInput);

        var inputs = $$"""{ "input": "{{jsonInputEncoded}}" }""".ToInputs();

        AssertQuerySuccess(query, expected, inputs);
    }
}

public class ArgumentDefaultValuesTests : QueryTestBase<VariablesSchema>
{
    [Fact]
    public void when_no_argument_provided()
    {
        const string query = """
            {
              fieldWithDefaultArgumentValue
            }
            """;

        const string expected = """
            {
              "fieldWithDefaultArgumentValue": "\"Hello World\""
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void when_nullable_variable_provided()
    {
        const string query = """
            query optionalVariable($optional:String) {
              fieldWithDefaultArgumentValue(input: $optional)
            }
            """;

        const string expected = """
            {
              "fieldWithDefaultArgumentValue": "\"Hello World\""
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void when_argument_provided_cannot_be_parsed()
    {
        const string query = """
{
  fieldWithDefaultArgumentValue(input: WRONG_TYPE)
}
""";

        var error = new ValidationError(default, ArgumentsOfCorrectTypeError.NUMBER, "Argument \u0027input\u0027 has invalid value. Expected type \u0027String\u0027, found WRONG_TYPE.")
        {
            Code = "ARGUMENTS_OF_CORRECT_TYPE",
        };
        error.AddLocation(new Location(2, 33));

        var expected = new ExecutionResult
        {
            Errors = [error],
        };

        AssertQueryIgnoreErrors(query, expected, renderErrors: true, expectedErrorCount: 1);
    }
}
