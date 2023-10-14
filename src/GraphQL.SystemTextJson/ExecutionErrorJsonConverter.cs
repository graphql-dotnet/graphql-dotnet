using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Execution;

namespace GraphQL.SystemTextJson;

/// <summary>
/// Converts an instance of <see cref="ExecutionError"/> to JSON. Doesn't support read from JSON.
/// </summary>
public class ExecutionErrorJsonConverter : JsonConverter<ExecutionError>
{
    private readonly IErrorInfoProvider _errorInfoProvider;

    /// <summary>
    /// Creates an instance of <see cref="ExecutionErrorJsonConverter"/> with the specified <see cref="IErrorInfoProvider"/>.
    /// </summary>
    public ExecutionErrorJsonConverter(IErrorInfoProvider errorInfoProvider)
    {
        _errorInfoProvider = errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider));
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ExecutionError error, JsonSerializerOptions options)
    {
        var info = _errorInfoProvider.GetInfo(error);

        writer.WriteStartObject();

        writer.WritePropertyName("message");

        JsonSerializer.Serialize(writer, info.Message, options);

        if (error.Locations != null)
        {
            writer.WritePropertyName("locations");
            writer.WriteStartArray();
            foreach (var location in error.Locations)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("line");
                JsonSerializer.Serialize(writer, location.Line, options);
                writer.WritePropertyName("column");
                JsonSerializer.Serialize(writer, location.Column, options);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (error.Path != null && error.Path.Any())
        {
            writer.WritePropertyName("path");
            JsonSerializer.Serialize(writer, error.Path, options);
        }

        if (info.Extensions?.Count > 0)
        {
            writer.WritePropertyName("extensions");
            JsonSerializer.Serialize(writer, info.Extensions, options);
        }

        writer.WriteEndObject();
    }

    /// <inheritdoc/>
    public override ExecutionError Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeof(ExecutionError).IsAssignableFrom(typeToConvert);
}
