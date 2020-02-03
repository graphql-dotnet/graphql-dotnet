using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// A custom JsonConverter for converting into a dictionary of objects of their real underlying type.
    /// </summary>
    /// <remarks>
    /// With thanks to @pekkah who shared this from tanka-graphql.
    /// </remarks>
    public class ObjectDictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return ReadDictionary(doc.RootElement);
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value,
            JsonSerializerOptions options)
        {
            //WriteDictionary(writer, value, options);
            var internalOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            JsonSerializer.Serialize(writer, value, internalOptions);
        }

        private Dictionary<string, object> ReadDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object>();
            foreach (var property in element.EnumerateObject())
            {
                var key = property.Name;
                var value = property.Value;
                object resultValue = null;

                switch (value.ValueKind)
                {
                    case JsonValueKind.Object:
                        resultValue = ReadDictionary(value);
                        break;
                    case JsonValueKind.Number:
                        resultValue = ReadNumber(value);
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        resultValue = value.GetBoolean();
                        break;
                    case JsonValueKind.String:
                        resultValue = value.GetString();
                        break;
                    case JsonValueKind.Null:
                        // default value is null
                        break;
                    case JsonValueKind.Array:
                        resultValue = ReadArray(value).ToList();
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected value kind: {value.ValueKind}");
                }

                result[key] = resultValue;
            }

            if (!result.Any())
                return null;

            return result;
        }

        private IEnumerable<object> ReadArray(JsonElement value)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.Object:
                        yield return ReadDictionary(item);
                        break;
                    case JsonValueKind.Number:
                        yield return ReadNumber(item);
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        yield return item.GetBoolean();
                        break;
                    case JsonValueKind.String:
                        yield return item.GetString();
                        break;
                    case JsonValueKind.Null:
                        yield return null;
                        break;
                    case JsonValueKind.Array:
                        yield return ReadArray(item).ToList();
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected value kind: {item.ValueKind}");
                }
            }
        }

        private object ReadNumber(JsonElement value)
        {
            if (value.TryGetInt32(out var i))
                return i;
            else if (value.TryGetInt64(out var l))
                return l;
            //else if (value.TryGetUInt64(out var ui))
            //    resultValue = ui;
            else if (BigInteger.TryParse(value.GetRawText(), out var bi))
                return bi;
            else if (value.TryGetDouble(out var d))
                return d;
            else if (value.TryGetDecimal(out var dd))
                return dd;

            throw new NotImplementedException($"Unexpected Number value. Raw text was: {value.GetRawText()}");
        }

        private void WriteDictionary(Utf8JsonWriter writer, Dictionary<string, object> dictionary,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var entry in dictionary)
            {
                var value = entry.Value;

                if (value == null)
                {
                    writer.WriteNull(entry.Key);
                    continue;
                }

                WriteProperty(writer, options, value, entry.Key);
            }

            writer.WriteEndObject();
        }

        private void WriteProperty(Utf8JsonWriter writer, JsonSerializerOptions options, object value, string key)
        {
            switch (value)
            {
                case int intValue:
                    writer.WriteNumber(key, intValue);
                    break;
                case double doubleValue:
                    writer.WriteNumber(key, doubleValue);
                    break;
                case decimal decimalValue:
                    writer.WriteNumber(key, decimalValue);
                    break;
                case string stringValue:
                    writer.WriteString(key, stringValue);
                    break;
                case bool boolValue:
                    writer.WriteBoolean(key, boolValue);
                    break;
                case Dictionary<string, object> subDictionary:
                    writer.WritePropertyName(key);
                    WriteDictionary(writer, subDictionary, options);
                    break;
                case IEnumerable list:
                    writer.WritePropertyName(key);
                    WriteArray(writer, list, options);
                    break;
                default:
                    JsonSerializer.Serialize(writer, value, value.GetType(), options);
                    break;
            }
        }

        private void WriteArray(Utf8JsonWriter writer, IEnumerable list, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var value in list)
            {
                WriteArrayValue(writer, value, options);
            }

            writer.WriteEndArray();
        }

        private void WriteArrayValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case int intValue:
                    writer.WriteNumberValue(intValue);
                    break;
                case double doubleValue:
                    writer.WriteNumberValue(doubleValue);
                    break;
                case decimal decimalValue:
                    writer.WriteNumberValue(decimalValue);
                    break;
                case string stringValue:
                    writer.WriteStringValue(stringValue);
                    break;
                case bool boolValue:
                    writer.WriteBooleanValue(boolValue);
                    break;
                case Dictionary<string, object> subDictionary:
                    WriteDictionary(writer, subDictionary, options);
                    break;
                case IEnumerable list:
                    WriteArray(writer, list, options);
                    break;
            }
        }
    }
}
