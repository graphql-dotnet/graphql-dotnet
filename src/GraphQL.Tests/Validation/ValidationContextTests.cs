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
        });
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
        });
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
        });
        ret.IsValid.ShouldBeFalse();
        ret.Errors.Count.ShouldBe(1);
        ret.Errors[0].Message.ShouldBe(errorMessage);
    }

    [Theory]
    [InlineData("query q01 ($arg: OneOfInput!) { test(arg: $arg) }", """{"arg":{"item1":"test"}}""")]
    [InlineData("query q02 ($arg: OneOfInput!) { test(arg: $arg) }", """{"arg":{"item2":"test"}}""")]
    [InlineData("query q03 ($arg: OneOfInput!) { test(arg: $arg) }", """{"arg":{"item1":"test","item2":"test"}}""", "Variable '$arg' is invalid. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("query q04 ($arg: OneOfInput!) { test(arg: $arg) }", """{"arg":{"item1":"test","item2":null}}""", "Variable '$arg' is invalid. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("query q05 ($arg: OneOfInput!) { test(arg: $arg) }", """{"arg":{"item1":null,"item2":"test"}}""", "Variable '$arg' is invalid. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("query q06 ($arg: OneOfInput!) { test(arg: $arg) }", """{"arg":{"item1":null,"item2":null}}""", "Variable '$arg' is invalid. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("query q07 ($arg: OneOfInput!) { test(arg: $arg) }", """{"arg":{"item1":null}}""", "Variable '$arg' is invalid. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("query q08 ($arg: String!) { test(arg: { item1: $arg } ) }", """{"arg":"test"}""")]
    [InlineData("query q09 ($arg: String) { test(arg: { item1: $arg } ) }", """{"arg":"test"}""")]
    [InlineData("query q10 ($arg: String!) { test(arg: { item1: $arg } ) }", """{"arg":null}""", "Variable '$arg' is invalid. Received a null input for a non-null variable.")]
    [InlineData("query q11 ($arg: String) { test(arg: { item1: $arg } ) }", """{"arg":null}""", "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    public async Task OneOfVariableCoercion(string query, string variables, string? expectedFailure = null)
    {
        var sdl = """
            input OneOfInput @oneOf {
              item1: String
              item2: String
            }

            type Query {
              test(arg: OneOfInput!): String
            }
            """;
        var schema = Schema.For(sdl);
        schema.Initialize();

        var validator = new DocumentValidator();
        var document = GraphQLParser.Parser.Parse(query);
        var ret = await validator.ValidateAsync(new ValidationOptions
        {
            Document = document,
            Schema = schema,
            Operation = document.Operation(),
            Variables = variables.ToInputs(),
        });

        if (expectedFailure == null)
        {
            ret.IsValid.ShouldBeTrue("Failure: " + ret.Errors.FirstOrDefault()?.Message);
        }
        else
        {
            ret.IsValid.ShouldBeFalse();
            (ret.Errors.FirstOrDefault()?.Message).ShouldBe(expectedFailure);
        }
    }

    [Theory]
    [InlineData("""query q01 { test(arg: { a: "abc", b: 123 }) }""", null, "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q02 { test(arg: { a: null, b: 123 }) }""", null, "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q03 { test(arg: { a: null, b: null }) }""", null, "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q04 { test(arg: { a: null }) }""", null, "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q05 { test(arg: { b: 123 }) }""", null)]
    [InlineData("""query q06 { test(arg: {}) }""", null, "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q07 ($a: String) { test(arg: { a: $a, b: 123 }) }""", """{"a":null}""", "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q07b ($a: String!) { test(arg: { a: $a, b: 123 }) }""", """{"a":null}""", "Variable '$a' is invalid. Received a null input for a non-null variable.")]
    [InlineData("""query q08 ($a: String) { test(arg: { a: $a, b: 123 }) }""", null, "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q08b ($a: String!) { test(arg: { a: $a, b: 123 }) }""", null, "Variable '$a' is invalid. No value provided for a non-null variable.")]
    [InlineData("""query q09 ($a: String, $b: Int) { test(arg: { a: $a, b: $b }) }""", """{"a":"abc"}""", "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q09b ($a: String!, $b: Int) { test(arg: { a: $a, b: $b }) }""", """{"a":"abc"}""", "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q09c ($a: String!, $b: Int!) { test(arg: { a: $a, b: $b }) }""", """{"a":"abc"}""", "Variable '$b' is invalid. No value provided for a non-null variable.")]
    [InlineData("""query q10 ($b: Int) { test(arg: { b: $b }) }""", """{"b":123}""")]
    [InlineData("""query q10b ($b: Int!) { test(arg: { b: $b }) }""", """{"b":123}""")]
    [InlineData("""query q11 ($var: ExampleInputTagged!) { test(arg: $var) }""", """{"var":{"b":123}}""")]
    [InlineData("""query q12 ($var: ExampleInputTagged!) { test(arg: $var) }""", """{"var":{"a":"abc","b":123}}""", "Variable '$var' is invalid. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q13 ($var: ExampleInputTagged!) { test(arg: $var) }""", """{"var":{"a":"abc","b":null}}""", "Variable '$var' is invalid. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q14 ($var: ExampleInputTagged!) { test(arg: $var) }""", """{"var":{"a":null}}""", "Variable '$var' is invalid. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q15 ($var: ExampleInputTagged!) { test(arg: $var) }""", """{"var":{}}""", "Variable '$var' is invalid. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q16 { test(arg: "abc123") }""", null, "Argument 'arg' has invalid value. Expected 'ExampleInputTagged', found not an object.")]
    [InlineData("""query q17 ($var: ExampleInputTagged!) { test(arg: $var) }""", """{"var":"abc123"}""", "Variable '$var' is invalid. Unable to parse input as a 'ExampleInputTagged' type. Did you provide a List or Scalar value accidentally?")]
    [InlineData("""query q18 { test(arg: { a: "abc", b: "123" }) }""", null, """Argument 'arg' has invalid value. In field 'b': [Expected type 'Int', found "123".]""")]
    [InlineData("""query q19 { test(arg: { b: "123" }) }""", null, "Argument 'arg' has invalid value. In field 'b': [Expected type 'Int', found \"123\".]")]
    [InlineData("""query q20 ($var: ExampleInputTagged!) { test(arg: $var) }""", """{"var":{"b":"abc"}}""", "Variable '$var.b' is invalid. Unable to convert 'abc' to 'Int'")]
    [InlineData("""query q21 { test(arg: { a: "abc" }) }""", null)]
    [InlineData("""query q22 ($b: Int) { test(arg: { b: $b }) }""", null, "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q22b ($b: Int!) { test(arg: { b: $b }) }""", null, "Variable '$b' is invalid. No value provided for a non-null variable.")]
    [InlineData("""query q23 ($var: ExampleInputTagged!) { test(arg: $var) }""", """{"var":{"a":"abc"}}""")]
    [InlineData("""query q24 { test(arg: { a: "abc", b: null }) }""", null, "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q25 ($b: Int) { test(arg: { b: $b }) }""", """{"b":null}""", "Invalid value for argument 'arg' of field 'test'. Input object literals mapping to a OneOf Input Object must contain exactly one non-null value.")]
    [InlineData("""query q25b ($b: Int!) { test(arg: { b: $b }) }""", """{"b":null}""", "Variable '$b' is invalid. Received a null input for a non-null variable.")]
    [InlineData("""query q26 { test(arg: { b: 123, c: "xyz" }) }""", null, "Argument 'arg' has invalid value. In field 'c': Unknown field.")]
    [InlineData("""query q27 ($var: ExampleInputTagged!) { test(arg: $var) }""", """{"var":{"b":123,"c":"xyz"}}""", "Variable '$var' is invalid. Unrecognized input fields 'c' for type 'ExampleInputTagged'.")]
    public async Task OneOfSamples(string query, string? variables, string? expectedFailure = null)
    {
        var sdl = """
            input ExampleInputTagged @oneOf {
              a: String
              b: Int
            }

            type Query {
              test(arg: ExampleInputTagged!): String
            }
            """;
        var schema = Schema.For(sdl); // also verifies schema-first @oneOf support
        schema.Initialize();

        var validator = new DocumentValidator();
        var document = GraphQLParser.Parser.Parse(query);
        var ret = await validator.ValidateAsync(new ValidationOptions
        {
            Document = document,
            Schema = schema,
            Operation = document.Operation(),
            Variables = variables.ToInputs(),
        });

        if (expectedFailure == null)
        {
            ret.IsValid.ShouldBeTrue("Failure: " + ret.Errors.FirstOrDefault()?.Message);
        }
        else
        {
            ret.IsValid.ShouldBeFalse();
            (ret.Errors.FirstOrDefault()?.Message).ShouldBe(expectedFailure);
        }
    }
}
