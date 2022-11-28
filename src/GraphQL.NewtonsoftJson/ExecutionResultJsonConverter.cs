using GraphQL.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.NewtonsoftJson;

/// <summary>
/// Converts an instance of <see cref="ExecutionResult"/> to JSON. Doesn't support read from JSON.
/// </summary>
public class ExecutionResultJsonConverter : JsonConverter
{
    private readonly NamingStrategy? _namingStrategy;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ExecutionResultJsonConverter()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified <see cref="NamingStrategy"/>.
    /// </summary>
    public ExecutionResultJsonConverter(NamingStrategy? namingStrategy)
    {
        _namingStrategy = namingStrategy;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var result = (ExecutionResult)value!;

        writer.WriteStartObject();

        WriteErrors(writer, result.Errors, serializer);
        WriteData(writer, result, serializer);
        WriteExtensions(writer, result, serializer);

        writer.WriteEndObject();
    }

    private void WriteData(JsonWriter writer, ExecutionResult result, JsonSerializer serializer)
    {
        if (result.Executed)
        {
            writer.WritePropertyName("data");
            if (result.Data is ExecutionNode executionNode)
            {
                WriteExecutionNode(writer, executionNode, serializer);
            }
            else
            {
                serializer.Serialize(writer, result.Data);
            }
        }
    }

    private void WriteExecutionNode(JsonWriter writer, ExecutionNode node, JsonSerializer serializer)
    {
        if (node is ValueExecutionNode valueExecutionNode)
        {
            serializer.Serialize(writer, valueExecutionNode.ToValue());
        }
        else if (node is ObjectExecutionNode objectExecutionNode)
        {
            if (objectExecutionNode.SubFields == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartObject();
                foreach (var childNode in objectExecutionNode.SubFields)
                {
                    var propName = childNode.Name!;
                    if (_namingStrategy != null)
                        propName = _namingStrategy.GetPropertyName(propName, false);
                    writer.WritePropertyName(propName);
                    WriteExecutionNode(writer, childNode, serializer);
                }
                writer.WriteEndObject();
            }
        }
        else if (node is ArrayExecutionNode arrayExecutionNode)
        {
            var items = arrayExecutionNode.Items;
            if (items == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartArray();
                foreach (var childNode in items)
                {
                    WriteExecutionNode(writer, childNode, serializer);
                }
                writer.WriteEndArray();
            }
        }
        else if (node == null || node is NullExecutionNode)
        {
            writer.WriteNull();
        }
        else
        {
            serializer.Serialize(writer, node.ToValue());
        }
    }

    private void WriteErrors(JsonWriter writer, ExecutionErrors? errors, JsonSerializer serializer)
    {
        if (errors == null || errors.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("errors");

        serializer.Serialize(writer, errors);
    }

    private void WriteExtensions(JsonWriter writer, ExecutionResult result, JsonSerializer serializer)
    {
        if (result.Extensions?.Count > 0)
        {
            writer.WritePropertyName("extensions");
            serializer.Serialize(writer, result.Extensions);
        }
    }

    /// <summary>
    /// This JSON converter does not support reading.
    /// </summary>
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override bool CanRead => false;

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => typeof(ExecutionResult).IsAssignableFrom(objectType);
}
