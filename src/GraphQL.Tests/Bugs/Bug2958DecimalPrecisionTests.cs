using System.Collections;
using GraphQL.Transport;

namespace GraphQL.Tests.Bugs;

public class Bug2958DecimalPrecisionTests : QueryTestBase<DecimalSchema>
{
    public class Bug2958DecimalPrecisionTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new SystemTextJson.GraphQLSerializer() };
            yield return new object[] { new NewtonsoftJson.GraphQLSerializer(settings => settings.FloatParseHandling = Newtonsoft.Json.FloatParseHandling.Decimal) };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(Bug2958DecimalPrecisionTestData))]
    public void double_to_decimal_does_not_lose_precision(IGraphQLTextSerializer serializer)
    {
        string request = @"{
""operationName"": ""TestMutation"",
""variables"": {
  ""input"": {
    ""discount"": 12345678901234.56
    }
},
""query"": ""mutation TestMutation($input: MutationType!)""
}";

        var inputs = serializer.Deserialize<GraphQLRequest>(request);
        inputs.Variables.ShouldNotBeNull();
        inputs.Variables.Count.ShouldBe(1);
        var inner = inputs.Variables["input"].ShouldBeOfType<Dictionary<string, object>>();
        inner.Count.ShouldBe(1);
        var value = inner["discount"];
        value.ShouldBeOfType<decimal>().ShouldBe(12345678901234.56m);
    }
}
