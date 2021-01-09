using System;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2159
    public class Bug2159 : QueryTestBase<Bug2159Schema>
    {
        private void AssertQueryWithError(string query, string result, string message, int line, int column, string path, Exception exception = null, string code = null, string inputs = null, string number = null)
            => AssertQueryWithError(query, result, message, line, column, new object[] { path }, exception, code, inputs, number);

        private void AssertQueryWithError(string query, string result, string message, int line, int column, object[] path, Exception exception = null, string code = null, string inputs = null, string number = null)
        {
            ExecutionError error;
            if (number != null)
                error = new ValidationError(null, number, message);
            else
                error = exception == null ? new ExecutionError(message) : new ExecutionError(message, exception);
            if (line != 0)
                error.AddLocation(line, column);
            error.Path = path;
            if (code != null)
                error.Code = code;
            var expected = CreateQueryResult(result, new ExecutionErrors { error });
            AssertQueryIgnoreErrors(query, expected, inputs?.ToInputs(), renderErrors: true, expectedErrorCount: 1);
        }

        [Fact]
        public void Direct_Literal_Default() => AssertQuerySuccess("{ testValue }", @"{ ""testValue"": ""defaultValue"" }");

        [Fact]
        public void Direct_Literal_Specified() => AssertQuerySuccess("{ testValue(arg: \"hello\") }", @"{ ""testValue"": ""hello"" }");

        [Fact]
        public void Direct_Literal_Null() => AssertQuerySuccess("{ testValue(arg: null) }", @"{ ""testValue"": null }");

        // If a variable is provided for an input object field, the runtime value of that variable must be used.
        // If the runtime value is null and the field type is non‐null, a field error must be thrown. If no runtime
        // value is provided, the variable definition’s default value should be used. If the variable definition
        // does not provide a default value, the input object field definition’s default value should be used.
        [Fact]
        public void Direct_Variable_FieldDefault() => AssertQuerySuccess("query($input: String) { testValue(arg: $input) }", @"{ ""testValue"": ""defaultValue"" }");

        [Fact]
        public void Direct_Variable_VarDefault() => AssertQuerySuccess("query($input: String = \"varDefault\") { testValue(arg: $input) }", @"{ ""testValue"": ""varDefault"" }");

        [Fact]
        public void Direct_Variable_Specified() => AssertQuerySuccess("query($input: String = \"varDefault\") { testValue(arg: $input) }", @"{ ""testValue"": ""hello"" }", "{\"input\":\"hello\"}".ToInputs());

        [Fact]
        public void Direct_Variable_Null() => AssertQuerySuccess("query($input: String = \"varDefault\") { testValue(arg: $input) }", @"{ ""testValue"": null }", "{\"input\":null}".ToInputs());


        [Fact]
        public void Object_Literal_Default() => AssertQuerySuccess("{ testObject }", @"{ ""testObject"": ""defaultValue"" }");

        [Fact]
        public void Object_Literal_Specified() => AssertQuerySuccess("{ testObject(arg: {value:\"hello\"}) }", @"{ ""testObject"": ""hello"" }");

        [Fact]
        public void Object_Literal_Null() => AssertQuerySuccess("{ testObject(arg: {value:null}) }", @"{ ""testObject"": null }");

        // If a variable is provided for an input object field, the runtime value of that variable must be used.
        // If the runtime value is null and the field type is non‐null, a field error must be thrown. If no runtime
        // value is provided, the variable definition’s default value should be used. If the variable definition
        // does not provide a default value, the input object field definition’s default value should be used.
        [Fact]
        public void Object_Variable_Default() => AssertQuerySuccess("query($input: Bug2159Object) { testObject(arg: $input) }", @"{ ""testObject"": ""defaultFieldValue"" }", "{\"input\":{}}".ToInputs());

        [Fact]
        public void Object_Variable_Specified() => AssertQuerySuccess("query($input: Bug2159Object) { testObject(arg: $input) }", @"{ ""testObject"": ""hello"" }", "{\"input\":{\"value\":\"hello\"}}".ToInputs());

        [Fact]
        public void Object_Variable_Null() => AssertQuerySuccess("query($input: Bug2159Object) { testObject(arg: $input) }", @"{ ""testObject"": null }", "{\"input\":{\"value\":null}}".ToInputs());
    }

    public class Bug2159Schema : Schema
    {
        public Bug2159Schema()
        {
            Query = new Bug2159Query();
        }
    }

    public class Bug2159Query : ObjectGraphType
    {
        public Bug2159Query()
        {
            Field<StringGraphType>(
                "testValue",
                resolve: ctx => ctx.GetArgument<string>("arg"),
                arguments: new QueryArguments(
                    new QueryArgument(typeof(StringGraphType)) { Name = "arg", DefaultValue = "defaultValue" }));
            Field<StringGraphType>(
                "testObject",
                resolve: ctx => ctx.GetArgument<Bug2159Object>("arg")?.Value,
                arguments: new QueryArguments(
                    new QueryArgument(typeof(Bug2159ObjectGraphType)) { Name = "arg", DefaultValue = new Bug2159Object { Value = "defaultValue" } }));
        }
    }

    public class Bug2159Object
    {
        public string Value { get; set; }
    }

    public class Bug2159ObjectGraphType : InputObjectGraphType<Bug2159Object>
    {
        public Bug2159ObjectGraphType()
        {
            Field(x => x.Value, true).DefaultValue("defaultFieldValue");
        }
    }
}
