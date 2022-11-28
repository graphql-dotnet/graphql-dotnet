using GraphQL.Execution;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson;

/// <summary>
/// Converts an instance of <see cref="ExecutionError"/> to JSON. Doesn't support read from JSON.
/// </summary>
public class ExecutionErrorJsonConverter : JsonConverter
{
    private readonly IErrorInfoProvider _errorInfoProvider;

    /// <summary>
    /// Initializes a new instance with the specified <see cref="IErrorInfoProvider"/>.
    /// </summary>
    public ExecutionErrorJsonConverter(IErrorInfoProvider errorInfoProvider)
    {
        _errorInfoProvider = errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider));
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var error = (ExecutionError)value!;
        var info = _errorInfoProvider.GetInfo(error);

        writer.WriteStartObject();

        writer.WritePropertyName("message");

        serializer.Serialize(writer, info.Message);

        if (error.Locations != null)
        {
            writer.WritePropertyName("locations");
            writer.WriteStartArray();
            foreach (var location in error.Locations)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("line");
                serializer.Serialize(writer, location.Line);
                writer.WritePropertyName("column");
                serializer.Serialize(writer, location.Column);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (error.Path != null && error.Path.Any())
        {
            writer.WritePropertyName("path");
            serializer.Serialize(writer, error.Path);
        }

        if (info.Extensions?.Count > 0)
        {
            writer.WritePropertyName("extensions");
            serializer.Serialize(writer, info.Extensions);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// This JSON converter does not support reading.
    /// </summary>
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override bool CanRead => false;

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => typeof(ExecutionError).IsAssignableFrom(objectType);
}
