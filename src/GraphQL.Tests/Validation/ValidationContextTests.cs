#nullable enable

using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Tests.Validation;

public class ValidationContextTests
{
    // the following test verifies the following specification rule:
    //
    // If the value passed as an input to a list type is not a list and not the null value,
    // then the result of input coercion is a list of size one, where the single item value
    // is the result of input coercion for the list’s item type on the provided value (note
    // this may apply recursively for nested lists).

    // it also tests to be sure that when a variable is used for a input object field, and
    // the variable is not supplied, that the input object field default value is used

    [Theory]
    [InlineData("query q01 { dummy }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    [InlineData("query q02 { dummy(arg: null) }", null, ArgumentSource.Literal, @"{""data"":{""dummy"":null}}")]
    [InlineData("query q03 { dummy(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query q04 ($arg: String){ dummy(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    [InlineData("query q05 ($arg: String){ dummy(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    [InlineData("query q06 ($arg: String){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    [InlineData("query q07 ($arg: String){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query q08 ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query q09 ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query q10 ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    [InlineData("query q11 ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]

    [InlineData("query q20 { dummyList }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    [InlineData("query q21 { dummyList(arg: null) }", null, ArgumentSource.Literal, @"{""data"":{""dummyList"":null}}")]
    [InlineData("query q22 { dummyList(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q23 ($arg: String){ dummyList(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    [InlineData("query q24 ($arg: String){ dummyList(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    [InlineData("query q25 ($arg: String){ dummyList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyList"":null}}")]
    [InlineData("query q26 ($arg: String){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q27 ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query q28 ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query q29 ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyList"":null}}")]
    [InlineData("query q30 ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q31 ($arg: String!){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q32 ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query q33 ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query q34 ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q35 ($arg: [String]!){ dummyList(arg: $arg) }", "{\"arg\":[\"test\"]}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q36 ($arg: [String]!){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]

    [InlineData("query q40 { dummyNestedList }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    [InlineData("query q41 { dummyNestedList(arg: null) }", null, ArgumentSource.Literal, @"{""data"":{""dummyNestedList"":null}}")]
    [InlineData("query q42 { dummyNestedList(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    [InlineData("query q43 ($arg: String){ dummyNestedList(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    [InlineData("query q44 ($arg: String){ dummyNestedList(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    [InlineData("query q45 ($arg: String){ dummyNestedList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":null}}")]
    [InlineData("query q46 ($arg: String){ dummyNestedList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    [InlineData("query q47 ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyNestedList"":[[""varDefault""]]}}")]
    [InlineData("query q48 ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyNestedList"":[[""varDefault""]]}}")]
    [InlineData("query q49 ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":null}}")]
    [InlineData("query q50 ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]

    [InlineData("query q60 { dummyObj (arg: { }) }", null, ArgumentSource.Literal, @"{""data"":{""dummyObj"":""objDefault""}}")]
    [InlineData("query q61 { dummyObj (arg: { item1: null }) }", null, ArgumentSource.Literal, @"{""data"":{""dummyObj"":null}}")]
    [InlineData("query q61 { dummyObj (arg: { item1: \"test\" }) }", null, ArgumentSource.Literal, @"{""data"":{""dummyObj"":""test""}}")]
    [InlineData("query q62 ($arg: String) { dummyObj (arg: { item1: $arg }) }", null, ArgumentSource.Literal, @"{""data"":{""dummyObj"":""objDefault""}}")]
    [InlineData("query q63 ($arg: String) { dummyObj (arg: { item1: $arg }) }", "{}", ArgumentSource.Literal, @"{""data"":{""dummyObj"":""objDefault""}}")]
    [InlineData("query q64 ($arg: String) { dummyObj (arg: { item1: $arg }) }", "{\"arg\":null}", ArgumentSource.Literal, @"{""data"":{""dummyObj"":null}}")]
    [InlineData("query q64 ($arg: String) { dummyObj (arg: { item1: $arg }) }", "{\"arg\":\"test\"}", ArgumentSource.Literal, @"{""data"":{""dummyObj"":""test""}}")]
    public async Task VariablesParseCorrectly(string query, string? variables, ArgumentSource expectedSource, string expectedResponse)
    {
        var dummyInputType = new InputObjectGraphType<DummyInput>
        {
            Name = "DummyInput",
        };
        dummyInputType.Field(x => x.Item1, true)
            .DefaultValue("objDefault");

        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<string>("dummy", true)
            .Argument<string>("arg", true, a => a.DefaultValue = "argDefault")
            .Resolve(context =>
            {
                var args = context.Arguments;
                var arg = args.ShouldHaveSingleItem();
                arg.Key.ShouldBe("arg");
                arg.Value.Source.ShouldBe(expectedSource);
                return (string?)arg.Value.Value;
            });
        queryType.Field<string>("dummyObj", true)
            .Argument<DummyInput>("arg", false, a => a.ResolvedType = new NonNullGraphType(dummyInputType))
            .Resolve(context =>
            {
                var args = context.Arguments;
                var arg = args.ShouldHaveSingleItem();
                arg.Key.ShouldBe("arg");
                arg.Value.Source.ShouldBe(expectedSource);
                var value = context.GetArgument<DummyInput>("arg");
                return value.Item1;
            });
        queryType.Field<IEnumerable<string>>("dummyList", true)
            .Argument<IEnumerable<string>>("arg", true, a => a.DefaultValue = new[] { "argDefault" })
            .Resolve(context =>
            {
                var args = context.Arguments;
                var arg = args.ShouldHaveSingleItem();
                arg.Key.ShouldBe("arg");
                arg.Value.Source.ShouldBe(expectedSource);
                return ((IEnumerable<object>?)arg.Value.Value)?.Cast<string>();
            });
        queryType.Field<IEnumerable<IEnumerable<string>?>>("dummyNestedList", true)
            .Argument<IEnumerable<IEnumerable<string>>>("arg", true, a => a.DefaultValue = new[] { new[] { "argDefault" } })
            .Resolve(context =>
            {
                var args = context.Arguments;
                var arg = args.ShouldHaveSingleItem();
                arg.Key.ShouldBe("arg");
                arg.Value.Source.ShouldBe(expectedSource);
                return ((IEnumerable<object?>?)arg.Value.Value)?.Select(x => ((IEnumerable<object>?)x)?.Cast<string>());
            });
        var schema = new Schema { Query = queryType };
        schema.Features.AllowScalarVariablesForListTypes = true;
        var serializer = new SystemTextJson.GraphQLSerializer();
        var response = await new DocumentExecuter().ExecuteAsync(new ExecutionOptions
        {
            Query = query,
            Schema = schema,
            Variables = serializer.Deserialize<Inputs>(variables),
            ThrowOnUnhandledException = true,
        }).ConfigureAwait(false);
        var responseJson = serializer.Serialize(response);
        responseJson.ShouldBeCrossPlatJson(expectedResponse);
    }

    private class DummyInput
    {
        public string? Item1 { get; set; }
    }

    private class DummyInputNonNull
    {
        public string Item1 { get; set; } = null!;
    }

    [Theory]
    [InlineData("query q01 { dummy }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    [InlineData("query q02 { dummy(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query q03 ($arg: String){ dummy(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    [InlineData("query q04 ($arg: String){ dummy(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    [InlineData("query q05 ($arg: String){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query q06 ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query q07 ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query q08 ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query q09 ($arg: String!){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query q10 ($arg: String! = \"varDefault\"){ dummy(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query q11 ($arg: String! = \"varDefault\"){ dummy(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query q12 ($arg: String! = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]

    [InlineData("query q20 { dummyNoDefault(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummyNoDefault"":""test""}}")]
    [InlineData("query q21 ($arg: String = \"varDefault\"){ dummyNoDefault(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyNoDefault"":""varDefault""}}")]
    [InlineData("query q22 ($arg: String = \"varDefault\"){ dummyNoDefault(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyNoDefault"":""varDefault""}}")]
    [InlineData("query q23 ($arg: String = \"varDefault\"){ dummyNoDefault(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNoDefault"":""test""}}")]
    [InlineData("query q24 ($arg: String!){ dummyNoDefault(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNoDefault"":""test""}}")]
    [InlineData("query q25 ($arg: String! = \"varDefault\"){ dummyNoDefault(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyNoDefault"":""varDefault""}}")]
    [InlineData("query q26 ($arg: String! = \"varDefault\"){ dummyNoDefault(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyNoDefault"":""varDefault""}}")]
    [InlineData("query q27 ($arg: String! = \"varDefault\"){ dummyNoDefault(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNoDefault"":""test""}}")]

    [InlineData("query q30 { dummyList }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    [InlineData("query q31 { dummyList(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q32 ($arg: String){ dummyList(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    [InlineData("query q33 ($arg: String){ dummyList(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    [InlineData("query q34 ($arg: String){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q35 ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query q36 ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query q37 ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q38 ($arg: String!){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q39 ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query q40 ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query q41 ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q42 ($arg: [String]!){ dummyList(arg: $arg) }", "{\"arg\":[\"test\"]}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q43 ($arg: [String]!){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query q44 { dummyNestedList }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    [InlineData("query q45 { dummyNestedList(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    [InlineData("query q46 ($arg: String){ dummyNestedList(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    [InlineData("query q47 ($arg: String){ dummyNestedList(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    [InlineData("query q48 ($arg: String){ dummyNestedList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    [InlineData("query q49 ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyNestedList"":[[""varDefault""]]}}")]
    [InlineData("query q50 ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyNestedList"":[[""varDefault""]]}}")]
    [InlineData("query q51 ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]

    [InlineData("query q60 { dummyObj (arg: { }) }", null, ArgumentSource.Literal, @"{""data"":{""dummyObj"":""objDefault""}}")]
    [InlineData("query q61 { dummyObj (arg: { item1: \"test\" }) }", null, ArgumentSource.Literal, @"{""data"":{""dummyObj"":""test""}}")]
    [InlineData("query q62 ($arg: String) { dummyObj (arg: { item1: $arg }) }", null, ArgumentSource.Literal, @"{""data"":{""dummyObj"":""objDefault""}}")]
    [InlineData("query q63 ($arg: String) { dummyObj (arg: { item1: $arg }) }", "{}", ArgumentSource.Literal, @"{""data"":{""dummyObj"":""objDefault""}}")]
    [InlineData("query q64 ($arg: String) { dummyObj (arg: { item1: $arg }) }", "{\"arg\":\"test\"}", ArgumentSource.Literal, @"{""data"":{""dummyObj"":""test""}}")]
    public async Task VariablesParseCorrectly_NonNull(string query, string? variables, ArgumentSource expectedSource, string expectedResponse)
    {
        var dummyInputType = new InputObjectGraphType<DummyInput>
        {
            Name = "DummyInput",
        };
        dummyInputType.Field(x => x.Item1, false)
            .DefaultValue("objDefault");

        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<string>("dummy", true)
            .Argument<string>("arg", false, a => a.DefaultValue = "argDefault")
            .Resolve(context =>
            {
                var args = context.Arguments;
                var arg = args.ShouldHaveSingleItem();
                arg.Key.ShouldBe("arg");
                arg.Value.Source.ShouldBe(expectedSource);
                return (string?)arg.Value.Value;
            });
        queryType.Field<string>("dummyObj", true)
            .Argument<DummyInput>("arg", false, a => a.ResolvedType = new NonNullGraphType(dummyInputType))
            .Resolve(context =>
            {
                var args = context.Arguments;
                var arg = args.ShouldHaveSingleItem();
                arg.Key.ShouldBe("arg");
                arg.Value.Source.ShouldBe(expectedSource);
                var value = context.GetArgument<DummyInput>("arg");
                return value.Item1;
            });
        queryType.Field<string>("dummyNoDefault", true)
            .Argument<string>("arg", false)
            .Resolve(context =>
            {
                var args = context.Arguments;
                var arg = args.ShouldHaveSingleItem();
                arg.Key.ShouldBe("arg");
                arg.Value.Source.ShouldBe(expectedSource);
                return (string?)arg.Value.Value;
            });
        queryType.Field<IEnumerable<string>>("dummyList", true)
            .Argument<IEnumerable<string>>("arg", false, a => a.DefaultValue = new[] { "argDefault" })
            .Resolve(context =>
            {
                var args = context.Arguments;
                var arg = args.ShouldHaveSingleItem();
                arg.Key.ShouldBe("arg");
                arg.Value.Source.ShouldBe(expectedSource);
                return ((IEnumerable<object>?)arg.Value.Value)?.Cast<string>();
            });
        queryType.Field<IEnumerable<IEnumerable<string>?>>("dummyNestedList", true)
            .Argument<IEnumerable<IEnumerable<string>>>("arg", false, a => a.DefaultValue = new[] { new[] { "argDefault" } })
            .Resolve(context =>
            {
                var args = context.Arguments;
                var arg = args.ShouldHaveSingleItem();
                arg.Key.ShouldBe("arg");
                arg.Value.Source.ShouldBe(expectedSource);
                return ((IEnumerable<object?>?)arg.Value.Value)?.Select(x => ((IEnumerable<object>?)x)?.Cast<string>());
            });
        var schema = new Schema { Query = queryType };
        schema.Features.AllowScalarVariablesForListTypes = true;
        var serializer = new SystemTextJson.GraphQLSerializer();
        var response = await new DocumentExecuter().ExecuteAsync(new ExecutionOptions
        {
            Query = query,
            Schema = schema,
            Variables = serializer.Deserialize<Inputs>(variables),
            ThrowOnUnhandledException = true,
        }).ConfigureAwait(false);
        var responseJson = serializer.Serialize(response);
        responseJson.ShouldBeCrossPlatJson(expectedResponse);
    }

    [Theory]
    // arg is non-null although it has a default value
    [InlineData("query q01 { dummy(arg: null) }", null,
        "Argument 'arg' has invalid value. Expected 'String!', found null.")]
    // q02 should fail because null was explicitly passed to a non-null argument
    [InlineData("query q02 ($arg: String) { dummy(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q03 { dummyNoDefault }", null,
        "Argument 'arg' of type 'String!' is required for field 'dummyNoDefault' but not provided.")]
    [InlineData("query q04 { dummyNoDefault(arg: null) }", null,
        "Argument 'arg' has invalid value. Expected 'String!', found null.")]
    [InlineData("query q05 ($arg: String) { dummyNoDefault(arg: $arg) }", null,
        "Variable '$arg' of type 'String' used in position expecting type 'String!'.")]
    [InlineData("query q06 ($arg: String) { dummyNoDefault(arg: $arg) }", "{}",
        "Variable '$arg' of type 'String' used in position expecting type 'String!'.")]
    [InlineData("query q07 ($arg: String) { dummyNoDefault(arg: $arg) }", null,
        "Variable '$arg' of type 'String' used in position expecting type 'String!'.")]
    [InlineData("query q08 ($arg: String!) { dummyNoDefault(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    // q09 should fail (passing test) because null was explicitly passed to a non-null argument,
    //   regardless of whether there is a variable default
    [InlineData("query q09 ($arg: String = \"varDefault\") { dummyNoDefault(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q10 { dummyList(arg: null) }", null,
        "Argument 'arg' has invalid value. Expected '!', found null.")]
    [InlineData("query q11 { dummyNestedList(arg: null) }", null,
        "Argument 'arg' has invalid value. Expected '!', found null.")]
    [InlineData("query q12 { dummyObj (arg: { item1: null }) }", null,
        "Argument 'arg' has invalid value. In field 'item1': [Expected 'String!', found null.]")]
    // q13 should also fail (passing test) because null was explicitly passed to a non-null object field
    [InlineData("query q13 ($arg: String) { dummyObj (arg: { item1: $arg }) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]

    [InlineData("query q31 { dummyList(arg: null) }", null,
        "Argument 'arg' has invalid value. Expected '!', found null.")]
    // note for all of these that the fact that the only error generated is the invalid variable
    //   error, indicating that it passed the rule that validates variable types (which it should)
    [InlineData("query q32 ($arg: String) { dummyList(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q33 ($arg: String = \"varDefault\") { dummyList(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q34 ($arg: String!) { dummyList(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q35 ($arg: [String] = [\"varDefault\"]) { dummyList(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q36 ($arg: [String]) { dummyList(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q37 ($arg: [String]!) { dummyList(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]

    [InlineData("query q41 { dummyListNoDefault(arg: null) }", null,
        "Argument 'arg' has invalid value. Expected '!', found null.")]
    [InlineData("query q42 ($arg: String) { dummyListNoDefault(arg: $arg) }", "{\"arg\":\"abc\"}",
        "Variable '$arg' of type 'String' used in position expecting type '[String]!'.")]
    [InlineData("query q43 ($arg: String = \"varDefault\") { dummyListNoDefault(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q44 ($arg: String!) { dummyListNoDefault(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q45 ($arg: [String] = [\"varDefault\"]) { dummyListNoDefault(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q46 ($arg: [String]) { dummyListNoDefault(arg: $arg) }", "{\"arg\":[]}",
        "Variable '$arg' of type '[String]' used in position expecting type '[String]!'.")]
    [InlineData("query q47 ($arg: [String]!) { dummyListNoDefault(arg: $arg) }", "{\"arg\":null}",
        "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    public async Task ScenariosThatFailValidationOrCoercion(string query, string? variables, string errorMessage)
    {
        var dummyInputType = new InputObjectGraphType<DummyInput>
        {
            Name = "DummyInput",
        };
        dummyInputType.Field(x => x.Item1, false)
            .DefaultValue("objDefault");

        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<string>("dummy", true)
            .Argument<string>("arg", false, a => a.DefaultValue = "argDefault");
        queryType.Field<string>("dummyObj", true)
            .Argument<DummyInput>("arg", false, a => a.ResolvedType = new NonNullGraphType(dummyInputType));
        queryType.Field<string>("dummyNoDefault", true)
            .Argument<string>("arg", false);
        queryType.Field<IEnumerable<string>>("dummyList", true)
            .Argument<IEnumerable<string>>("arg", false, a => a.DefaultValue = new[] { "argDefault" });
        queryType.Field<IEnumerable<string>>("dummyListNoDefault", true)
            .Argument<IEnumerable<string>>("arg", false);
        queryType.Field<IEnumerable<IEnumerable<string>?>>("dummyNestedList", true)
            .Argument<IEnumerable<IEnumerable<string>>>("arg", false, a => a.DefaultValue = new[] { new[] { "argDefault" } });
        var schema = new Schema { Query = queryType };
        schema.Features.AllowScalarVariablesForListTypes = true;
        schema.Initialize();
        var document = GraphQLParser.Parser.Parse(query);
        var validator = new DocumentValidator();
        var ret = await validator.ValidateAsync(new ValidationOptions
        {
            Document = document,
            Schema = schema,
            Operation = document.Operation(),
            Variables = variables.ToInputs(),
        }).ConfigureAwait(false);
        ret.IsValid.ShouldBeFalse();
        ret.Errors.Count.ShouldBe(1);
        ret.Errors[0].Message.ShouldBe(errorMessage);
    }
}
