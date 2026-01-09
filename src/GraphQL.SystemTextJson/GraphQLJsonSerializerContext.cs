#if NET8_0_OR_GREATER
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Transport;

namespace GraphQL.SystemTextJson;

/// <summary>
/// Default JSON serializer context for GraphQL types with AOT support.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never
)]
// Simple GraphQL.NET types
[JsonSerializable(typeof(ApolloTrace))]
// Common intrinsic types used in GraphQL.NET
// Note that other scalar graph types like DateTimeGraphType serializes data to a string, so no special handling is needed here
[JsonSerializable(typeof(JsonElement?))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(BigInteger))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(sbyte))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(char))]
[JsonSerializable(typeof(string))]
// Required if serializing Inputs (not strictly needed for GraphQL operation)
[JsonSerializable(typeof(IReadOnlyDictionary<string, object?>))]
// Required when serializing output Extensions
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(List<object?>))]
[JsonSerializable(typeof(object[]))]
// Other types that may be used during GraphQL processing
[JsonSerializable(typeof(IEnumerable<object?>))]
[JsonSerializable(typeof(IDictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
[JsonSerializable(typeof(List<string?>))]
internal partial class GraphQLJsonSerializerContext : JsonSerializerContext
{
}

// this class handles types that cannot properly be handled by source generation
// but are already supported by custom converters
internal class GraphQLCustomJsonSerializerContext : JsonSerializerContext, IJsonTypeInfoResolver
{
    public GraphQLCustomJsonSerializerContext(IErrorInfoProvider errorInfoProvider) : base(CreateOptions(errorInfoProvider))
    {
    }

    private static JsonSerializerOptions CreateOptions(IErrorInfoProvider errorInfoProvider)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ExecutionResultJsonConverter());
        options.Converters.Add(new ExecutionErrorJsonConverter(errorInfoProvider));
        options.Converters.Add(new GraphQLRequestJsonConverter());
        options.Converters.Add(new GraphQLRequestListJsonConverter());
        options.Converters.Add(new InputsJsonConverter());
        options.Converters.Add(new OperationMessageJsonConverter());
        options.Converters.Add(new JsonConverterBigInteger());
        return options;
    }

    /// <inheritdoc/>
    protected override JsonSerializerOptions? GeneratedSerializerOptions => Options;

    /// <inheritdoc/>
    public override JsonTypeInfo? GetTypeInfo(Type type)
    {
        Options.TryGetTypeInfo(type, out JsonTypeInfo? typeInfo);
        return typeInfo;
    }

    JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        // supported types by ExecutionResultJsonConverter
        if (type == typeof(ExecutionResult))
            return CreateTypeInfo<ExecutionResult>(type, options);
        // supported types by ExecutionErrorJsonConverter
        if (type == typeof(ExecutionError))
            return CreateTypeInfo<ExecutionError>(type, options);
        // supported types by GraphQLRequestJsonConverter
        if (type == typeof(GraphQLRequest))
            return CreateTypeInfo<GraphQLRequest>(type, options);
        // supported types by GraphQLRequestListJsonConverter
        if (type == typeof(IList<GraphQLRequest>))
            return CreateTypeInfo<IList<GraphQLRequest>>(type, options);
        if (type == typeof(GraphQLRequest[]))
            return CreateTypeInfo<GraphQLRequest[]>(type, options);
        if (type == typeof(List<GraphQLRequest>))
            return CreateTypeInfo<List<GraphQLRequest>>(type, options);
        if (type == typeof(IEnumerable<GraphQLRequest>))
            return CreateTypeInfo<IEnumerable<GraphQLRequest>>(type, options);
        if (type == typeof(ICollection<GraphQLRequest>))
            return CreateTypeInfo<ICollection<GraphQLRequest>>(type, options);
        if (type == typeof(IReadOnlyCollection<GraphQLRequest>))
            return CreateTypeInfo<IReadOnlyCollection<GraphQLRequest>>(type, options);
        if (type == typeof(IReadOnlyList<GraphQLRequest>))
            return CreateTypeInfo<IReadOnlyList<GraphQLRequest>>(type, options);
        // supported types by InputsJsonConverter
        if (type == typeof(Inputs))
            return CreateTypeInfo<Inputs>(type, options);
        // supported types by OperationMessageJsonConverter
        if (type == typeof(OperationMessage))
            return CreateTypeInfo<OperationMessage>(type, options);
        // support ExecutionErrors
        if (type == typeof(ExecutionErrors))
            return CreateExecutionErrorsInfo(options);

        return null;
    }

    private static JsonTypeInfo? CreateTypeInfo<TJsonMetadataType>(Type type, JsonSerializerOptions options)
    {
        for (int i = 0; i < options.Converters.Count; i++)
        {
            JsonConverter? converter = options.Converters[i];
            if (converter?.CanConvert(type) == true)
            {
                // Note: this code is not designed to be compatible with JsonConverterFactory
                return JsonMetadataServices.CreateValueInfo<TJsonMetadataType>(options, converter);
            }
        }

        return null;
    }

    private static JsonTypeInfo CreateExecutionErrorsInfo(JsonSerializerOptions options)
    {
        var executionErrorTypeInfo = options.GetTypeInfo(typeof(ExecutionError));
        var info = new JsonCollectionInfoValues<ExecutionErrors>
        {
            ObjectCreator = () => new ExecutionErrors(),
            SerializeHandler = (writer, value) =>
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }
                writer.WriteStartArray();
                foreach (var error in value)
                {
                    JsonSerializer.Serialize(writer, error, executionErrorTypeInfo);
                }
                writer.WriteEndArray();
            }
        };
        var jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<ExecutionErrors, ExecutionError>(options, info);
        jsonTypeInfo.NumberHandling = null;
        return jsonTypeInfo;
    }
}
#endif
