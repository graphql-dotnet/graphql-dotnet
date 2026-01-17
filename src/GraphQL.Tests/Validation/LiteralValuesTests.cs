#nullable enable

using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class LiteralValuesTests
{
    [Theory]
    [InlineData("{ str (arg: 3) }", null, false, "INVALID_LITERAL", "Invalid literal for argument 'arg' of field 'str'. Unable to convert '3' literal from AST representation to the scalar type 'String'", 1, 13)]
    [InlineData("{ str (arg: 3) }", null, true, "ARGUMENTS_OF_CORRECT_TYPE", "Argument 'arg' has invalid value. Expected type 'String', found 3.", 1, 8)]
    [InlineData("{ str (arg2: {}) }", null, false, "INVALID_LITERAL", "Invalid literal for argument 'arg2' of field 'str'. Error parsing dictionary", 1, 14)]
    [InlineData("{ str (arg2: {}) }", null, true, "INVALID_LITERAL", "Invalid literal for argument 'arg2' of field 'str'. Error parsing dictionary", 1, 14)]
    [InlineData("""{ str (arg3: "3") }""", null, false, "INVALID_LITERAL", "Invalid literal for argument 'arg3' of field 'str'. Could not parse date. Expected yyyy-MM-dd. Value: 3", 1, 14)]
    [InlineData("""{ str (arg3: "3") }""", null, true, "ARGUMENTS_OF_CORRECT_TYPE", "Argument 'arg3' has invalid value. Expected type 'Date', found \"3\".", 1, 8)]
    [InlineData("""{ str (arg3: {}) }""", null, false, "INVALID_LITERAL", "Invalid literal for argument 'arg3' of field 'str'. Unable to convert 'GraphQLParser.AST.GraphQLObjectValueFull' literal from AST representation to the scalar type 'Date'", 1, 14)]
    [InlineData("""{ str (arg3: {}) }""", null, true, "ARGUMENTS_OF_CORRECT_TYPE", "Argument 'arg3' has invalid value. Expected type 'Date', found {}.", 1, 8)]
    [InlineData("""{ str (arg4: "3") }""", null, false, "INVALID_LITERAL", "Invalid literal for argument 'arg4' of field 'str'. Error parsing test literal", 1, 14)]
    [InlineData("""{ str (arg4: "3") }""", null, true, "INVALID_LITERAL", "Invalid literal for argument 'arg4' of field 'str'. Error parsing test literal", 1, 14)]
    [InlineData("query ($arg: String = 3) { str (arg: $arg) }", null, false, "INVALID_LITERAL", "Invalid literal for node. Unable to convert '3' literal from AST representation to the scalar type 'String'", 1, 23)]
    [InlineData("query ($arg: String = 3) { str (arg: $arg) }", null, true, "DEFAULT_VALUES_OF_CORRECT_TYPE", "Variable 'arg' of type 'String' has invalid default value '3'. Expected type 'String', found 3.", 1, 23)]
    [InlineData("""query ($arg3: Date = "3") { str (arg3: $arg3) }""", null, false, "INVALID_LITERAL", "Invalid literal for node. Could not parse date. Expected yyyy-MM-dd. Value: 3", 1, 22)]
    [InlineData("""query ($arg3: Date = "3") { str (arg3: $arg3) }""", null, true, "DEFAULT_VALUES_OF_CORRECT_TYPE", "Variable 'arg3' of type 'Date' has invalid default value '\"3\"'. Expected type 'Date', found \"3\".", 1, 22)]
    [InlineData("""query ($arg4: Test = "3") { str (arg4: $arg4) }""", null, false, "INVALID_LITERAL", "Invalid literal for node. Error parsing test literal", 1, 22)]
    [InlineData("""query ($arg4: Test = "3") { str (arg4: $arg4) }""", null, true, "INVALID_LITERAL", "Invalid literal for node. Error parsing test literal", 1, 22)]
    public async Task Test_Input(string query, string? variables, bool useRules, string code, string message, int line, int column)
    {
        var schema = new MySchema();
        schema.Initialize();
        var document = Parser.Parse(query);
        var validator = new DocumentValidator();
        var ret = await validator.ValidateAsync(new()
        {
            Document = document,
            Schema = schema,
            Operation = document.Operation(),
            Rules = useRules ? null : Array.Empty<IValidationRule>(),
            Variables = variables?.ToInputs() ?? Inputs.Empty,
        });
        var err = ret.Errors.ShouldHaveSingleItem();
        err.Code.ShouldBe(code);
        err.Message.ShouldBe(message);
        var location = err.Locations.ShouldHaveSingleItem();
        location.Line.ShouldBe(line);
        location.Column.ShouldBe(column);
        err.Path.ShouldBeNull();
    }

    private class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
        }
    }

    private class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Name = "Query";
            Field<StringGraphType>("str")
                .Argument<StringGraphType>("arg")
                .Argument<MyInputGraphType>("arg2")
                .Argument<DateGraphType>("arg3")
                .Argument<MyScalarGraphType>("arg4")
                .Resolve(_ => null);
        }
    }

    public class MyScalarGraphType : ScalarGraphType
    {
        public MyScalarGraphType()
        {
            Name = "Test";
        }

        public override object? ParseLiteral(GraphQLValue value)
            => throw new InvalidOperationException("Error parsing test literal");

        public override bool CanParseLiteral(GraphQLValue value) => true;

        public override object? ParseValue(object? value)
            => throw new InvalidOperationException("Error parsing test value");

        public override bool CanParseValue(object? value) => true;
    }

    public class MyInputGraphType : InputObjectGraphType
    {
        public MyInputGraphType()
        {
            Name = "MyInput";
            Field<StringGraphType>("str");
        }

        public override object ParseDictionary(IDictionary<string, object?> value, IValueConverter valueConverter)
            => throw new InvalidOperationException("Error parsing dictionary");
    }
}
