using System.Text.Json;
using GraphQL.Execution;
using GraphQL.SystemTextJson;

namespace GraphQL.Tests.Serialization.SystemTextJson;

public class GraphQLSerializerTests
{
#if NET5_0_OR_GREATER
    [Fact]
    public void DoesNotModifyOptions_1()
    {
        var options = new JsonSerializerOptions();
        _ = JsonSerializer.Serialize("hello", options);
        options.Converters.Any(x => x.CanConvert(typeof(Inputs))).ShouldBeFalse();
        var serializer = new GraphQLSerializer(options);
        _ = serializer.Serialize("hello");
        options.Converters.Any(x => x.CanConvert(typeof(Inputs))).ShouldBeFalse();
    }

    [Fact]
    public void DoesNotModifyOptions_2()
    {
        var options = new JsonSerializerOptions();
        _ = JsonSerializer.Serialize("hello", options);
        options.Converters.Any(x => x.CanConvert(typeof(Inputs))).ShouldBeFalse();
        var serializer = new GraphQLSerializer(options, new ErrorInfoProvider());
        _ = serializer.Serialize("hello");
        options.Converters.Any(x => x.CanConvert(typeof(Inputs))).ShouldBeFalse();
    }
#else
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
#endif
}
