using GraphQL.NewtonsoftJson;
using GraphQL.Transport;

namespace GraphQL.Tests.Serialization.NewtonsoftJson;

public class GraphQLRequestListJsonConverterTests
{
    [Theory]
    [InlineData(typeof(IEnumerable<GraphQLRequest>), true)]
    [InlineData(typeof(ICollection<GraphQLRequest>), true)]
    [InlineData(typeof(IReadOnlyCollection<GraphQLRequest>), true)]
    [InlineData(typeof(IReadOnlyList<GraphQLRequest>), true)]
    [InlineData(typeof(IList<GraphQLRequest>), true)]
    [InlineData(typeof(List<GraphQLRequest>), true)]
    [InlineData(typeof(GraphQLRequest[]), true)]
    [InlineData(typeof(IEnumerable<string>), false)]
    public void CanConvert_Works(Type type, bool expected)
    {
        var converter = new GraphQLRequestListJsonConverter();
        converter.CanConvert(type).ShouldBe(expected);
    }
}
