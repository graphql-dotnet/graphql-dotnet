using System;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/1699
    public class Bug1699InvalidEnum : QueryTestBase<Bug1699InvalidEnumSchema>
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
            if (line != 0) error.AddLocation(line, column);
            error.Path = path;
            if (code != null)
                error.Code = code;
            var expected = CreateQueryResult(result, new ExecutionErrors { error });
            AssertQueryIgnoreErrors(query, expected, inputs?.ToInputs(), renderErrors: true, expectedErrorCount: 1);
        }

        [Fact]
        public void Simple_Enum() => AssertQuerySuccess("{ grumpy }", @"{ ""grumpy"": ""GRUMPY"" }");

        [Fact]
        public void String_Enum() => AssertQuerySuccess("{ sleepy }", @"{ ""sleepy"": ""SLEEPY"" }");

        // within C#, (int)Bug1699Enum.Happy does not equal Bug1699.Happy
        [Fact]
        public void Int_Enum() => AssertQueryWithError("{ happy }", @"{ ""happy"": null }", "Error trying to resolve field 'happy'.", 1, 3, "happy", exception: new InvalidOperationException());

        [Fact]
        public void Invalid_Enum() => AssertQueryWithError("{ invalidEnum }", @"{ ""invalidEnum"": null }", "Error trying to resolve field 'invalidEnum'.", 1, 3, "invalidEnum", exception: new InvalidOperationException());

        // TODO: does not yet fully meet spec (does not return members of the enum that are able to be serialized, with nulls and individual errors for unserializable values)
        [Fact]
        public void Invalid_Enum_Within_List() => AssertQueryWithError("{ invalidEnumWithinList }", @"{ ""invalidEnumWithinList"": null }", "Error trying to resolve field 'invalidEnumWithinList'.", 1, 3, "invalidEnumWithinList", exception: new InvalidOperationException());

        [Fact]
        public void Invalid_Enum_Within_NonNullList() => AssertQueryWithError("{ invalidEnumWithinNonNullList }", @"{ ""invalidEnumWithinNonNullList"": null }", "Error trying to resolve field 'invalidEnumWithinNonNullList'.", 1, 3, "invalidEnumWithinNonNullList", exception: new InvalidOperationException());

        [Fact]
        public void Input_Enum_Valid() => AssertQuerySuccess("{ inputEnum(arg: SLEEPY) }", @"{ ""inputEnum"": ""SLEEPY"" }");

        [Fact]
        public void Input_Enum_Valid2() => AssertQuerySuccess("{ input(arg: SLEEPY) }", @"{ ""input"": ""Sleepy"" }");

        [Fact]
        public void Input_Enum_InvalidEnum() => AssertQueryWithError("{ input(arg: DOPEY) }", null, "Argument \u0022arg\u0022 has invalid value DOPEY.\nExpected type \u0022Bug1699Enum\u0022, found DOPEY.", 1, 9, (object[])null, code: "ARGUMENTS_OF_CORRECT_TYPE", number: ArgumentsOfCorrectTypeError.PARAGRAPH);

        [Fact]
        public void Input_Enum_InvalidString() => AssertQueryWithError(@"{ input(arg: ""SLEEPY"") }", null, "Argument \u0022arg\u0022 has invalid value \u0022SLEEPY\u0022.\nExpected type \u0022Bug1699Enum\u0022, found \u0022SLEEPY\u0022.", 1, 9, (object[])null, code: "ARGUMENTS_OF_CORRECT_TYPE", number: ArgumentsOfCorrectTypeError.PARAGRAPH);

        [Fact]
        public void Input_Enum_InvalidInt() => AssertQueryWithError(@"{ input(arg: 2) }", null, "Argument \u0022arg\u0022 has invalid value 2.\nExpected type \u0022Bug1699Enum\u0022, found 2.", 1, 9, (object[])null, code: "ARGUMENTS_OF_CORRECT_TYPE", number: ArgumentsOfCorrectTypeError.PARAGRAPH);

        [Fact]
        public void Input_Enum_Valid_Variable() => AssertQuerySuccess("query($arg: Bug1699Enum!) { input(arg: $arg) }", @"{ ""input"": ""Grumpy"" }", "{\"arg\":\"GRUMPY\"}".ToInputs());

        [Fact]
        public void Input_Enum_InvalidEnum_Variable() => AssertQueryWithError(@"query($arg: Bug1699Enum!) { input(arg: $arg) }", null, "Variable \u0027$arg\u0027 is invalid. Unable to convert \u0027DOPEY\u0027 to \u0027Bug1699Enum\u0027", 1, 7, (object[])null, code: "INVALID_VALUE", inputs: "{\"arg\":\"DOPEY\"}");

        [Fact]
        public void Input_Enum_InvalidInt_Variable() => AssertQueryWithError(@"query($arg: Bug1699Enum!) { input(arg: $arg) }", null, "Variable \u0027$arg\u0027 is invalid. Unable to convert \u00272\u0027 to \u0027Bug1699Enum\u0027", 1, 7, (object[])null, code: "INVALID_VALUE", inputs: "{\"arg\":2}");

        [Fact]
        public void Input_Enum_UndefinedDefault() => AssertQuerySuccess("{ input }", @"{ ""input"": ""Grumpy"" }");

        [Fact]
        public void Input_Enum_SetDefault() => AssertQuerySuccess("{ inputWithDefault }", @"{ ""inputWithDefault"": ""Happy"" }");

        [Fact]
        public void Input_Enum_OverrideDefault() => AssertQuerySuccess("{ inputWithDefault(arg: SLEEPY) }", @"{ ""inputWithDefault"": ""Sleepy"" }");

        [Fact]
        public void Input_Enum_MissingRequired() => AssertQueryWithError(@"{ inputRequired }", null, "Argument \u0022arg\u0022 of type \u0022Bug1699Enum!\u0022 is required for field \u0022inputRequired\u0022 but not provided.", 1, 3, (object[])null, code: "PROVIDED_NON_NULL_ARGUMENTS", number: ProvidedNonNullArgumentsError.PARAGRAPH);

        [Fact]
        public void Input_Enum_RequiredWithDefault() => AssertQuerySuccess("{ inputRequiredWithDefault }", @"{ ""inputRequiredWithDefault"": ""Happy"" }");
    }

    public class Bug1699InvalidEnumSchema : Schema
    {
        public Bug1699InvalidEnumSchema()
        {
            Query = new Bug1699InvalidEnumQuery();
        }
    }

    public class Bug1699InvalidEnumQuery : ObjectGraphType
    {
        public Bug1699InvalidEnumQuery()
        {
            Field<EnumerationGraphType<Bug1699Enum>>(
                "grumpy",
                resolve: ctx => Bug1699Enum.Grumpy);
            Field<EnumerationGraphType<Bug1699Enum>>(
                "happy",
                resolve: ctx => (int)Bug1699Enum.Happy);
            Field<EnumerationGraphType<Bug1699Enum>>(
                "sleepy",
                resolve: ctx => Bug1699Enum.Sleepy.ToString());
            Field<EnumerationGraphType<Bug1699Enum>>(
                "invalidEnum",
                resolve: ctx => 50);
            Field<ListGraphType<EnumerationGraphType<Bug1699Enum>>>(
                "invalidEnumWithinList",
                resolve: ctx => new Bug1699Enum[] { Bug1699Enum.Happy, Bug1699Enum.Sleepy, (Bug1699Enum)50 });
            Field<ListGraphType<NonNullGraphType<EnumerationGraphType<Bug1699Enum>>>>(
                "invalidEnumWithinNonNullList",
                resolve: ctx => new Bug1699Enum[] { Bug1699Enum.Happy, Bug1699Enum.Sleepy, (Bug1699Enum)50 });
            Field<EnumerationGraphType<Bug1699Enum>>(
                "inputEnum",
                arguments: new QueryArguments(new QueryArgument<EnumerationGraphType<Bug1699Enum>> { Name = "arg" }),
                resolve: ctx => ctx.GetArgument<Bug1699Enum>("arg"));
            Field<StringGraphType>(
                "input",
                arguments: new QueryArguments(new QueryArgument<EnumerationGraphType<Bug1699Enum>> { Name = "arg" }),
                resolve: ctx => ctx.GetArgument<Bug1699Enum>("arg").ToString());
            Field<StringGraphType>(
                "inputWithDefault",
                arguments: new QueryArguments(new QueryArgument<EnumerationGraphType<Bug1699Enum>> { Name = "arg", DefaultValue = Bug1699Enum.Happy }),
                resolve: ctx => ctx.GetArgument<Bug1699Enum>("arg").ToString());
            Field<StringGraphType>(
                "inputRequired",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<EnumerationGraphType<Bug1699Enum>>> { Name = "arg" }),
                resolve: ctx => ctx.GetArgument<Bug1699Enum>("arg").ToString());
            Field<StringGraphType>(
                "inputRequiredWithDefault",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<EnumerationGraphType<Bug1699Enum>>> { Name = "arg", DefaultValue = Bug1699Enum.Happy }),
                resolve: ctx => ctx.GetArgument<Bug1699Enum>("arg").ToString());
        }
    }

    public enum Bug1699Enum
    {
        Grumpy,
        Happy,
        Sleepy,
    }
}
