using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Instrumentation;

namespace GraphQL.SystemTextJson;

/// <summary>
/// Converts an instance of <see cref="ApolloTrace"/> to/from JSON.
/// </summary>
public class ApolloTraceJsonConverter : JsonConverter<ApolloTrace>
{
    private readonly JsonSerializerOptions _optionsNoIndent = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly JsonSerializerOptions _optionsIndent = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [UnconditionalSuppressMessage("Trimming", "IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT")]
    public override void Write(Utf8JsonWriter writer, ApolloTrace value, JsonSerializerOptions options) // options ignored, this is by design to enforce camelCase
        => JsonSerializer.Serialize(writer, value, options.WriteIndented ? _optionsIndent : _optionsNoIndent);

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [UnconditionalSuppressMessage("Trimming", "IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT")]
    public override ApolloTrace Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<ApolloTrace>(ref reader)!;
}
