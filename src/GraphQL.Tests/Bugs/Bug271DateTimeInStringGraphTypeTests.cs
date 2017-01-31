using GraphQL.Tests.Execution;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug271DateTimeInStringGraphTypeTests : QueryTestBase<VariablesSchema>
    {
        [Fact]
        public void supports_date_inputs_inside_string_fields()
        {
            var query = @"query q($input: TestInputObject) {
                              fieldWithObjectInput(input: $input)
                            }";
            
            var expected = "{ \"fieldWithObjectInput\": \"{\\\"a\\\":\\\"2017-01-27T15:19:53.000Z\\\",\\\"b\\\":[\\\"bar\\\"],\\\"c\\\":\\\"baz\\\"}\" }";
            var inputs = "{'input': {'a':'2017-01-27T15:19:53.000Z', 'b':['bar'], 'c': 'baz'} }".ToInputs();
            AssertQuerySuccess(query, expected, inputs);
        }
    }
}
