using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/2557
public class Bug2557 : QueryTestBase<Bug2557.MySchema>
{
    [Fact]
    public void NoDefault_NoArg() => AssertQuerySuccess("{ noDefault }", @"{ ""noDefault"": ""getArgumentDefault"" }");

    [Fact]
    public void NoDefault_LiteralArg() => AssertQuerySuccess(@"{ noDefault (arg: ""test"") }", @"{ ""noDefault"": ""test"" }");

    [Fact]
    public void NoDefault_LiteralNullArg() => AssertQuerySuccess("{ noDefault (arg: null) }", @"{ ""noDefault"": ""getArgumentDefault"" }");

    [Fact]
    public void NoDefault_VariableDefaultUnspecifiedArg() => AssertQuerySuccess(@"query($arg: String) { noDefault (arg: $arg) }", @"{ ""noDefault"": ""getArgumentDefault"" }");

    [Fact]
    public void NoDefault_VariableDefaultNullArg() => AssertQuerySuccess(@"query($arg: String = null) { noDefault (arg: $arg) }", @"{ ""noDefault"": ""getArgumentDefault"" }");

    [Fact]
    public void NoDefault_VariableDefaultArg() => AssertQuerySuccess(@"query($arg: String = ""test"") { noDefault (arg: $arg) }", @"{ ""noDefault"": ""test"" }");

    [Fact]
    public void NoDefault_VariableArg() => AssertQuerySuccess(@"query($arg: String) { noDefault (arg: $arg) }", @"{ ""noDefault"": ""test"" }", @"{ ""arg"": ""test"" }".ToInputs());

    [Fact]
    public void NoDefault_VariableNullArg() => AssertQuerySuccess(@"query($arg: String) { noDefault (arg: $arg) }", @"{ ""noDefault"": ""getArgumentDefault"" }", @"{ ""arg"": null }".ToInputs());

    [Fact]
    public void WithDefault_NoArg() => AssertQuerySuccess("{ withDefault }", @"{ ""withDefault"": ""fieldDefault"" }");

    [Fact]
    public void WithDefault_LiteralArg() => AssertQuerySuccess(@"{ withDefault (arg: ""test"") }", @"{ ""withDefault"": ""test"" }");

    [Fact]
    public void WithDefault_LiteralNullArg() => AssertQuerySuccess("{ withDefault (arg: null) }", @"{ ""withDefault"": ""getArgumentDefault"" }");

    [Fact]
    public void WithDefault_VariableDefaultUnspecifiedArg() => AssertQuerySuccess(@"query($arg: String) { withDefault (arg: $arg) }", @"{ ""withDefault"": ""fieldDefault"" }");

    [Fact]
    public void WithDefault_VariableDefaultNullArg() => AssertQuerySuccess(@"query($arg: String = null) { withDefault (arg: $arg) }", @"{ ""withDefault"": ""getArgumentDefault"" }");

    [Fact]
    public void WithDefault_VariableDefaultArg() => AssertQuerySuccess(@"query($arg: String = ""test"") { withDefault (arg: $arg) }", @"{ ""withDefault"": ""test"" }");

    [Fact]
    public void WithDefault_VariableArg() => AssertQuerySuccess(@"query($arg: String) { withDefault (arg: $arg) }", @"{ ""withDefault"": ""test"" }", @"{ ""arg"": ""test"" }".ToInputs());

    [Fact]
    public void WithDefault_VariableNullArg() => AssertQuerySuccess(@"query($arg: String) { withDefault (arg: $arg) }", @"{ ""withDefault"": ""getArgumentDefault"" }", @"{ ""arg"": null }".ToInputs());

    public class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<StringGraphType>(
                "noDefault",
                resolve: ctx => ctx.GetArgument<string>("arg", "getArgumentDefault"),
                arguments: new QueryArguments { new QueryArgument(typeof(StringGraphType)) { Name = "arg" } });
            Field<StringGraphType>(
                "withDefault",
                resolve: ctx => ctx.GetArgument<string>("arg", "getArgumentDefault"),
                arguments: new QueryArguments { new QueryArgument(typeof(StringGraphType)) { Name = "arg", DefaultValue = "fieldDefault" } });
        }
    }
}
