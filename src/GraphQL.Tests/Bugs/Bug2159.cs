using GraphQL.SystemTextJson;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2159
    public class Bug2159 : QueryTestBase<Bug2159Schema>
    {
        [Fact]
        public void Direct_Literal_Default() => AssertQuerySuccess("{ testValue }", @"{ ""testValue"": ""defaultValue"" }");

        [Fact]
        public void Direct_Literal_Specified() => AssertQuerySuccess("{ testValue(arg: \"hello\") }", @"{ ""testValue"": ""hello"" }");

        [Fact]
        public void Direct_Literal_Null() => AssertQuerySuccess("{ testValue(arg: null) }", @"{ ""testValue"": null }");

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
