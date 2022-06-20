#pragma warning disable IDE0005 // Using directive is unnecessary.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Types;

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

    [Fact]
    public async Task CanOverrideDecimalDataType()
    {
        var result = await new DocumentExecuter().ExecuteAsync(new ExecutionOptions
        {
            Schema = new Schema { Query = new AutoRegisteringObjectGraphType<Query>() },
            Query = "{hero}"
        }).ConfigureAwait(false);

        // typically a value of 1.0000m would be serialized as 1.0000
        new GraphQLSerializer().Serialize(result).ShouldBe(@"{""data"":{""hero"":1.0000}}");

        // tests that a custom converter can be registered for the decimal data type; here it serializes 1.0000m as 1
        new GraphQLSerializer(o => o.Converters.Add(new SampleDecimalConverter())).Serialize(result).ShouldBe(@"{""data"":{""hero"":1}}");
    }

    private class SampleDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
#if NET6_0_OR_GREATER
            => writer.WriteRawValue(value.ToString("0.#############", CultureInfo.InvariantCulture));
#else
            => writer.WriteNumberValue((double)value);
#endif
    }

    private class Query
    {
        public static decimal Hero => 1.0000m;
    }
}
