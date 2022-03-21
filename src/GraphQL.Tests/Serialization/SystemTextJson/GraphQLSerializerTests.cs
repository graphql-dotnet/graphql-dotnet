using System.Text.Json;
using GraphQL.SystemTextJson;

namespace GraphQL.Tests.Serialization.SystemTextJson;

public class GraphQLSerializerTests
{
    [Fact]
    public void GraphQLSerializer_Should_Be_Created_With_Same_Options_Multiple_Times()
    {
        var options = new JsonSerializerOptions();
        var serializer = new GraphQLSerializer(options);
        options.Converters.Count.ShouldBe(8);
        _ = serializer.Serialize<object>(new { name = "Tom", age = 42 });
        new GraphQLSerializer(options);
        options.Converters.Count.ShouldBe(8);
    }

#if NET5_0_OR_GREATER
    [Fact]
    public void DoesNotModifyOptions1()
    {
        var options = new JsonSerializerOptions();
        _ = JsonSerializer.Serialize("hello", options);
        var serializer = new GraphQLSerializer(options);
        _ = serializer.Serialize("hello");
    }
#endif
}
