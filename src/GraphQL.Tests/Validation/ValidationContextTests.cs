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
    [Theory]
    [InlineData("{ dummy }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    [InlineData("{ dummy(arg: null) }", null, ArgumentSource.Literal, @"{""data"":{""dummy"":null}}")]
    [InlineData("{ dummy(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query ($arg: String){ dummy(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    [InlineData("query ($arg: String){ dummy(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    [InlineData("query ($arg: String){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    [InlineData("query ($arg: String){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("{ dummyList }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    [InlineData("{ dummyList(arg: null) }", null, ArgumentSource.Literal, @"{""data"":{""dummyList"":null}}")]
    [InlineData("{ dummyList(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: String){ dummyList(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    [InlineData("query ($arg: String){ dummyList(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    [InlineData("query ($arg: String){ dummyList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyList"":null}}")]
    [InlineData("query ($arg: String){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyList"":null}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: String!){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: [String]!){ dummyList(arg: $arg) }", "{\"arg\":[\"test\"]}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: [String]!){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("{ dummyNestedList }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    [InlineData("{ dummyNestedList(arg: null) }", null, ArgumentSource.Literal, @"{""data"":{""dummyNestedList"":null}}")]
    [InlineData("{ dummyNestedList(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    [InlineData("query ($arg: String){ dummyNestedList(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    [InlineData("query ($arg: String){ dummyNestedList(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    [InlineData("query ($arg: String){ dummyNestedList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":null}}")]
    [InlineData("query ($arg: String){ dummyNestedList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyNestedList"":[[""varDefault""]]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyNestedList"":[[""varDefault""]]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":null}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    public async Task VariablesParseCorrectly(string query, string? variables, ArgumentSource expectedSource, string expectedResponse)
    {
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
    [InlineData("{ dummy }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    //[InlineData("{ dummy(arg: null) }", null, ArgumentSource.Literal, @"{""data"":{""dummy"":null}}")]
    [InlineData("{ dummy(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummy"":""test""}}")]
    //[InlineData("query ($arg: String){ dummy(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    //[InlineData("query ($arg: String){ dummy(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    //[InlineData("query ($arg: String){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    //[InlineData("query ($arg: String){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    //[InlineData("query ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    //[InlineData("query ($arg: String = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    //[InlineData("query ($arg: String!){ dummy(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    //[InlineData("query ($arg: String!){ dummy(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    //[InlineData("query ($arg: String!){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    [InlineData("query ($arg: String!){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("query ($arg: String! = \"varDefault\"){ dummy(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    [InlineData("query ($arg: String! = \"varDefault\"){ dummy(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummy"":""varDefault""}}")]
    //[InlineData("query ($arg: String! = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    [InlineData("query ($arg: String! = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummy"":""test""}}")]
    [InlineData("{ dummyList }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    //[InlineData("{ dummyList(arg: null) }", null, ArgumentSource.Literal, @"{""data"":{""dummyList"":null}}")]
    [InlineData("{ dummyList(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummyList"":[""test""]}}")]
    //[InlineData("query ($arg: String){ dummyList(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    //[InlineData("query ($arg: String){ dummyList(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummyList"":[""argDefault""]}}")]
    //[InlineData("query ($arg: String){ dummyList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyList"":null}}")]
    //[InlineData("query ($arg: String){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    //[InlineData("query ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyList"":null}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: String!){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyList"":[""varDefault""]}}")]
    [InlineData("query ($arg: String! = \"varDefault\"){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: [String]!){ dummyList(arg: $arg) }", "{\"arg\":[\"test\"]}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("query ($arg: [String]!){ dummyList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyList"":[""test""]}}")]
    [InlineData("{ dummyNestedList }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    //[InlineData("{ dummyNestedList(arg: null) }", null, ArgumentSource.Literal, @"{""data"":{""dummyNestedList"":null}}")]
    [InlineData("{ dummyNestedList(arg: \"test\") }", null, ArgumentSource.Literal, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    //[InlineData("query ($arg: String){ dummyNestedList(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    //[InlineData("query ($arg: String){ dummyNestedList(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummyNestedList"":[[""argDefault""]]}}")]
    //[InlineData("query ($arg: String){ dummyNestedList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":null}}")]
    //[InlineData("query ($arg: String){ dummyNestedList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", null, ArgumentSource.VariableDefault, @"{""data"":{""dummyNestedList"":[[""varDefault""]]}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{}", ArgumentSource.VariableDefault, @"{""data"":{""dummyNestedList"":[[""varDefault""]]}}")]
    //[InlineData("query ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":null}}")]
    [InlineData("query ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }", "{\"arg\":\"test\"}", ArgumentSource.Variable, @"{""data"":{""dummyNestedList"":[[""test""]]}}")]
    public async Task VariablesParseCorrectly_NonNull(string query, string? variables, ArgumentSource expectedSource, string expectedResponse)
    {
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
    [InlineData("{ dummy(arg: null) }")]
    [InlineData("query ($arg: String){ dummy(arg: $arg) }")] //WRONG
    //[InlineData("query ($arg: String = \"varDefault\"){ dummy(arg: $arg) }")]
    //[InlineData("query ($arg: String!){ dummy(arg: $arg) }", null, ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    //[InlineData("query ($arg: String!){ dummy(arg: $arg) }", "{}", ArgumentSource.FieldDefault, @"{""data"":{""dummy"":""argDefault""}}")]
    //[InlineData("query ($arg: String!){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    //[InlineData("query ($arg: String! = \"varDefault\"){ dummy(arg: $arg) }", "{\"arg\":null}", ArgumentSource.Variable, @"{""data"":{""dummy"":null}}")]
    [InlineData("{ dummyList(arg: null) }")]
    [InlineData("query ($arg: String){ dummyList(arg: $arg) }")]
    //[InlineData("query ($arg: String = \"varDefault\"){ dummyList(arg: $arg) }")]
    [InlineData("{ dummyNestedList(arg: null) }")]
    [InlineData("query ($arg: String){ dummyNestedList(arg: $arg) }")]
    //[InlineData("query ($arg: String = \"varDefault\"){ dummyNestedList(arg: $arg) }")]
    public async Task ScenariosThatFailBasicValidation(string query)
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<string>("dummy", true)
            .Argument<string>("arg", false, a => a.DefaultValue = "argDefault");
        queryType.Field<IEnumerable<string>>("dummyList", true)
            .Argument<IEnumerable<string>>("arg", false, a => a.DefaultValue = new[] { "argDefault" });
        queryType.Field<IEnumerable<IEnumerable<string>?>>("dummyNestedList", true)
            .Argument<IEnumerable<IEnumerable<string>>>("arg", false, a => a.DefaultValue = new[] { new[] { "argDefault" } });
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        var document = GraphQLParser.Parser.Parse(query);
        var validator = new DocumentValidator();
        var ret = await validator.ValidateAsync(new ValidationOptions
        {
            Document = document,
            Schema = schema,
            Operation = document.Operation(),
            Variables = Inputs.Empty,
        }).ConfigureAwait(false);
        ret.validationResult.IsValid.ShouldBeFalse();
    }
}
