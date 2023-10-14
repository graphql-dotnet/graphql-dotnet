using GraphQL.Tests.Execution;

namespace GraphQL.Tests.Bugs;

public class Bug271DateTimeInStringGraphTypeTests : QueryTestBase<VariablesSchema>
{
    [Fact]
    public void supports_date_inputs_inside_string_fields()
    {
        const string query = "query q($input: TestInputObject) { fieldWithObjectInput(input: $input) }";
        const string expected = """{ "fieldWithObjectInput": "{\"a\":\"2017-01-27T15:19:53.000Z\",\"b\":[\"bar\"],\"c\":\"baz\"}" }""";
        var inputs = """{ "input": { "a": "2017-01-27T15:19:53.000Z", "b": ["bar"], "c": "baz"} }""".ToInputs();
        AssertQuerySuccess(query, expected, inputs);
    }
}
