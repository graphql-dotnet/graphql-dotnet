using GraphQL.SystemTextJson;
using GraphQL.Types;
using System;
using System.Linq;
using System.Numerics;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pulls/1766
    public class Bug1766InvalidByte : QueryTestBase<Bug1766Schema>
    {
        private void AssertQueryWithError(string query, string result, string message, int line, int column, object[] path, Exception exception = null, string code = null, string inputs = null)
        {
            var error = exception == null ? new ExecutionError(message) : new ExecutionError(message, exception);
            if (line != 0) error.AddLocation(line, column);
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
        public void Input_Byte_Invalid_Variable() => AssertQueryWithError("query($arg: Byte!) { input(arg: $arg) }", null, "Variable '$arg' is invalid. Unable to convert '300' to 'Byte'", 1, 7, null, null, "INVALID_VALUE", "{\"arg\":300}");

        [Fact]
        public void Input_Byte_Invalid_Argument() => AssertQueryWithError("query { input(arg: 300) }", null, "Argument \"arg\" has invalid value 300.\nExpected type \"Byte\", found 300.", 1, 15, null, code: "5.3.3.1");
    }

    public class Bug1766Schema : Schema
    {
        public Bug1766Schema()
        {
            Query = new Bug1766Query();
        }
    }

    public class Bug1766Query : ObjectGraphType
    {
        public Bug1766Query()
        {
            Field<StringGraphType>(
                "input",
                arguments: new QueryArguments(new QueryArgument<ByteGraphType> { Name = "arg" }),
                resolve: ctx => ctx.GetArgument<byte>("arg").ToString());
        }
    }
}
