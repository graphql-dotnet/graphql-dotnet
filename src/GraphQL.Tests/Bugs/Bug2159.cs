using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

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

    [Fact]
    public void HasArgument_NoDefault_None() => AssertQuerySuccess("{ hasArgumentNoDefault }", @"{ ""hasArgumentNoDefault"": false }");

    [Fact]
    public void HasArgument_NoDefault_UnsetVariable() => AssertQuerySuccess("query($input: Boolean) { hasArgumentNoDefault(arg: $input) }", @"{ ""hasArgumentNoDefault"": false }");

    [Fact]
    public void HasArgument_NoDefault_Set() => AssertQuerySuccess("{ hasArgumentNoDefault(arg: true) }", @"{ ""hasArgumentNoDefault"": true }");

    [Fact]
    public void HasArgument_NoDefault_DefaultVariable() => AssertQuerySuccess("query($input: Boolean = true) { hasArgumentNoDefault(arg: $input) }", @"{ ""hasArgumentNoDefault"": true }");

    [Fact]
    public void HasArgument_NoDefault_SetVariable() => AssertQuerySuccess("query($input: Boolean) { hasArgumentNoDefault(arg: $input) }", @"{ ""hasArgumentNoDefault"": true }", "{\"input\":true}".ToInputs());

    [Fact]
    public void HasArgument_NoDefault_SetNull() => AssertQuerySuccess("{ hasArgumentNoDefault(arg: null) }", @"{ ""hasArgumentNoDefault"": true }");

    [Fact]
    public void HasArgument_NoDefault_DefaultVariableNull() => AssertQuerySuccess("query($input: Boolean = null) { hasArgumentNoDefault(arg: $input) }", @"{ ""hasArgumentNoDefault"": true }");

    [Fact]
    public void HasArgument_NoDefault_SetVariableNull() => AssertQuerySuccess("query($input: Boolean) { hasArgumentNoDefault(arg: $input) }", @"{ ""hasArgumentNoDefault"": true }", "{\"input\":null}".ToInputs());

    [Fact]
    public void HasArgument_WithDefault_None() => AssertQuerySuccess("{ hasArgumentWithDefault }", @"{ ""hasArgumentWithDefault"": false }");

    [Fact]
    public void HasArgument_WithDefault_UnsetVariable() => AssertQuerySuccess("query($input: Boolean) { hasArgumentWithDefault(arg: $input) }", @"{ ""hasArgumentWithDefault"": false }");

    [Fact]
    public void HasArgument_WithDefault_Set() => AssertQuerySuccess("{ hasArgumentWithDefault(arg: true) }", @"{ ""hasArgumentWithDefault"": true }");

    [Fact]
    public void HasArgument_WithDefault_DefaultVariable() => AssertQuerySuccess("query($input: Boolean = true) { hasArgumentWithDefault(arg: $input) }", @"{ ""hasArgumentWithDefault"": true }");

    [Fact]
    public void HasArgument_WithDefault_SetVariable() => AssertQuerySuccess("query($input: Boolean) { hasArgumentWithDefault(arg: $input) }", @"{ ""hasArgumentWithDefault"": true }", "{\"input\":true}".ToInputs());

    [Fact]
    public void HasArgument_WithDefault_SetNull() => AssertQuerySuccess("{ hasArgumentWithDefault(arg: null) }", @"{ ""hasArgumentWithDefault"": true }");

    [Fact]
    public void HasArgument_WithDefault_DefaultVariableNull() => AssertQuerySuccess("query($input: Boolean = null) { hasArgumentWithDefault(arg: $input) }", @"{ ""hasArgumentWithDefault"": true }");

    [Fact]
    public void HasArgument_WithDefault_SetVariableNull() => AssertQuerySuccess("query($input: Boolean) { hasArgumentWithDefault(arg: $input) }", @"{ ""hasArgumentWithDefault"": true }", "{\"input\":null}".ToInputs());

    [Fact]
    public void CheckRequiredObjectFieldAreRequired_Variable() => AssertQueryWithErrors("query($input: Bug2159ReqObj) { testReqObjField(arg: $input) }", "null", "{\"input\":{\"value2\":null}}".ToInputs(), expectedErrorCount: 1, executed: false);

    [Fact]
    public void CheckRequiredObjectFieldAreRequired_Literal() => AssertQueryWithErrors("{ testReqObjField(arg: { value2: null }) }", "null", expectedErrorCount: 1, executed: false);
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
        Field<StringGraphType>("testValue")
            .Argument(typeof(StringGraphType), "arg", arg => arg.DefaultValue = "defaultValue")
            .Resolve(ctx => ctx.GetArgument<string>("arg"));
        Field<StringGraphType>("testObject")
            .Argument(typeof(Bug2159ObjectGraphType), "arg", arg => arg.DefaultValue = new Bug2159Object { Value = "defaultValue" })
            .Resolve(ctx => ctx.GetArgument<Bug2159Object>("arg")?.Value);
        Field<BooleanGraphType>("hasArgumentNoDefault")
            .Argument(typeof(BooleanGraphType), "arg")
            .Resolve(ctx => ctx.HasArgument("arg"));
        Field<BooleanGraphType>("hasArgumentWithDefault")
            .Argument(typeof(BooleanGraphType), "arg", arg => arg.DefaultValue = true)
            .Resolve(ctx => ctx.HasArgument("arg"));
        Field<StringGraphType>("testReqObjField")
            .Argument(typeof(Bug2159ReqObjGraphType), "arg")
            .Resolve(_ => "OK");
    }
}

public class Bug2159Object
{
    public string Value { get; set; }
    public string Value2 { get; set; }
}

public class Bug2159ObjectGraphType : InputObjectGraphType<Bug2159Object>
{
    public Bug2159ObjectGraphType()
    {
        Field(x => x.Value, true).DefaultValue("defaultFieldValue");
    }
}

public class Bug2159ReqObjGraphType : InputObjectGraphType<Bug2159Object>
{
    public Bug2159ReqObjGraphType()
    {
        Field(x => x.Value);
        Field(x => x.Value2, true);
    }
}
