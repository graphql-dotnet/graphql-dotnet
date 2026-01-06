#if NET8_0_OR_GREATER
using System.Diagnostics;
#endif
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Execution;

namespace GraphQL.SystemTextJson;

/// <summary>
/// Converts an instance of <see cref="ExecutionResult"/> to JSON. Doesn't support read from JSON.
/// </summary>
public class ExecutionResultJsonConverter : JsonConverter<ExecutionResult>
{
    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [UnconditionalSuppressMessage("Trimming", "IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT")]
    public override void Write(Utf8JsonWriter writer, ExecutionResult value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Important: Be careful with passing the same options down when recursively calling Serialize.
        // See docs: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to
        WriteErrors(writer, value.Errors, options);
        WriteData(writer, value, options);
        WriteExtensions(writer, value, options);

        writer.WriteEndObject();
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)")]
    private static void WriteData(Utf8JsonWriter writer, ExecutionResult result, JsonSerializerOptions options)
    {
        if (result.Executed)
        {
            writer.WritePropertyName("data");
            if (result.Data is ExecutionNode executionNode)
            {
                WriteExecutionNode(writer, executionNode, options);
            }
            else
            {
                JsonSerializer.Serialize(writer, result.Data, options);
            }
        }
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)")]
    private static void WriteExecutionNode(Utf8JsonWriter writer, ExecutionNode node, JsonSerializerOptions options)
    {
        if (node is ValueExecutionNode valueExecutionNode)
        {
            JsonSerializer.Serialize(writer, valueExecutionNode.ToValue(), options);
        }
        else if (node is ObjectExecutionNode objectExecutionNode)
        {
            if (objectExecutionNode.SubFields == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStartObject();
                foreach (var childNode in objectExecutionNode.SubFields)
                {
                    var propertyName = childNode.Name!;
                    if (options.PropertyNamingPolicy != null)
                        propertyName = options.PropertyNamingPolicy.ConvertName(propertyName);
                    writer.WritePropertyName(propertyName);
                    WriteExecutionNode(writer, childNode, options);
                }
                writer.WriteEndObject();
            }
        }
        else if (node is ArrayExecutionNode arrayExecutionNode)
        {
            var items = arrayExecutionNode.Items;
            if (items == null)
            {
                var serializedResult = arrayExecutionNode.SerializedResult;
                if (serializedResult == null)
                {
                    writer.WriteNullValue();
                }
                else if (serializedResult.GetType() == typeof(byte[]))
                {
                    // For array execution nodes, if the serialized result is a byte array, write it as an array of numbers
                    writer.WriteStartArray();
                    foreach (var b in (byte[])serializedResult)
                    {
                        writer.WriteNumberValue(b);
                    }
                    writer.WriteEndArray();
                }
                else
                {
#if !NET8_0_OR_GREATER
                    // Specify object? to eliminate boxing when iterating over IEnumerable
                    JsonSerializer.Serialize<object?>(writer, serializedResult, options);
#else
                    if (options.TryGetTypeInfo(serializedResult.GetType(), out var typeInfo))
                    {
                        JsonSerializer.Serialize(writer, serializedResult, typeInfo);
                    }
                    else
                    {
                        // Arrays of primitive types may not have type info available, so fall back to manual serialization
                        writer.WriteStartArray();
                        foreach (var item in serializedResult)
                        {
                            JsonSerializer.Serialize(writer, item, options);
                        }
                        writer.WriteEndArray();
                    }
#endif
                }
            }
            else
            {
                writer.WriteStartArray();
                foreach (var childNode in items)
                {
                    WriteExecutionNode(writer, childNode, options);
                }
                writer.WriteEndArray();
            }
        }
        else if (node == null || node is NullExecutionNode)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, node.ToValue(), options);
        }
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)")]
    private static void WriteErrors(Utf8JsonWriter writer, ExecutionErrors? errors, JsonSerializerOptions options)
    {
        if (errors == null || errors.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("errors");

        JsonSerializer.Serialize(writer, errors, options);
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)")]
    private static void WriteExtensions(Utf8JsonWriter writer, ExecutionResult result, JsonSerializerOptions options)
    {
        if (result.Extensions?.Count > 0)
        {
            writer.WritePropertyName("extensions");
            JsonSerializer.Serialize(writer, result.Extensions, options);
        }
    }

    /// <inheritdoc/>
    public override ExecutionResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeof(ExecutionResult).IsAssignableFrom(typeToConvert);
}
