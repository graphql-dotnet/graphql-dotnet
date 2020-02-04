using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// A custom JsonConverter for reading a dictionary of objects of their real underlying type.
    /// </summary>
    /// <remarks>
    /// Based on @pekkah's from tanka-graphql.
    /// </remarks>
    public class ObjectDictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);

            if (doc?.RootElement == null || doc?.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("This converter can only parse when the root element is a JSON Object.");
            }

            return ReadDictionary(doc.RootElement);
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
            => throw new NotImplementedException(
                "This converter currently is only intended to be used to read a JSON object into a strongly-typed representation.");

        private Dictionary<string, object> ReadDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object>();
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ReadValue(property.Value);
            }
            return result;
        }

        private IEnumerable<object> ReadArray(JsonElement value)
        {
            foreach (var item in value.EnumerateArray())
            {
                yield return ReadValue(item);
            }
        }

        private object ReadValue(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    return ReadDictionary(value);
                case JsonValueKind.Number:
                    return ReadNumber(value);
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return value.GetBoolean();
                case JsonValueKind.String:
                    return value.GetString();
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Array:
                    return ReadArray(value).ToList();
                default:
                    throw new InvalidOperationException($"Unexpected value kind: {value.ValueKind}");
            }
        }

        private object ReadNumber(JsonElement value)
        {
            if (value.TryGetInt32(out var i))
                return i;
            else if (value.TryGetInt64(out var l))
                return l;
            else if (BigInteger.TryParse(value.GetRawText(), out var bi))
                return bi;
            else if (value.TryGetDouble(out var d))
                return d;
            else if (value.TryGetDecimal(out var dd))
                return dd;

            throw new NotImplementedException($"Unexpected Number value. Raw text was: {value.GetRawText()}");
        }
    }
}
