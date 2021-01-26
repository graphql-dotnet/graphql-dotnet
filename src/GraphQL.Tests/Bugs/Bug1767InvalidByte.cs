using System;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pulls/1767
    public class Bug1767InvalidByte : QueryTestBase<Bug1767Schema>
    {
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
        public void Input_Byte_Valid_Variable() => AssertQuerySuccess("query($arg: Byte!) { input(arg: $arg) }", @"{ ""input"": ""2"" }", "{\"arg\":2}".ToInputs());

        [Fact]
        public void Input_Byte_Valid_Argument() => AssertQuerySuccess("query { input(arg: 2) }", @"{ ""input"": ""2"" }", "{\"arg\":2}".ToInputs());

        [Fact]
        public void Input_Byte_Invalid_Variable() => AssertQueryWithError("query($arg: Byte!) { input(arg: $arg) }", null, "Variable '$arg' is invalid. Unable to convert '300' to 'Byte'", 1, 7, null, new OverflowException(), "INVALID_VALUE", "{\"arg\":300}");

        [Fact]
        public void Input_Byte_Invalid_Argument() => AssertQueryWithError("query { input(arg: 300) }", null, "Argument \"arg\" has invalid value 300.\nExpected type \"Byte\", found 300.", 1, 15, null, code: "ARGUMENTS_OF_CORRECT_TYPE", number: ArgumentsOfCorrectTypeError.NUMBER);
    }

    public class Bug1767Schema : Schema
    {
        public Bug1767Schema()
        {
            Query = new Bug1767Query();
        }
    }

    public class Bug1767Query : ObjectGraphType
    {
        public Bug1767Query()
        {
            Field<StringGraphType>(
                "input",
                arguments: new QueryArguments(new QueryArgument<ByteGraphType> { Name = "arg" }),
                resolve: ctx => ctx.GetArgument<byte>("arg").ToString());
        }
    }
}
