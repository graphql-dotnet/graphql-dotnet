using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// A custom JsonConverter for reading a dictionary of objects of their real underlying type.
    /// Doesn't support write.
    /// </summary>
    public class ObjectDictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
            => throw new NotImplementedException(
                "This converter currently is only intended to be used to read a JSON object into a strongly-typed representation.");

        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ReadDictionary(ref reader);

        private static Dictionary<string, object> ReadDictionary(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var result = new Dictionary<string, object>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                string key = reader.GetString();

                // move to property value
                if (!reader.Read())
                    throw new JsonException();

                result.Add(key, ReadValue(ref reader));
            }

            return result;
        }

        private static object ReadValue(ref Utf8JsonReader reader)
            => reader.TokenType switch
            {
                JsonTokenType.StartArray => ReadArray(ref reader),
                JsonTokenType.StartObject => ReadDictionary(ref reader),
                JsonTokenType.Number => ReadNumber(ref reader),
                JsonTokenType.True => BoolBox.True,
                JsonTokenType.False => BoolBox.False,
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Null => null,
                JsonTokenType.None => null,
                _ => throw new InvalidOperationException($"Unexpected token type: {reader.TokenType}")
            };

        private static List<object> ReadArray(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            var result = new List<object>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                result.Add(ReadValue(ref reader));
            }

            return result;
        }

        private static object ReadNumber(ref Utf8JsonReader reader)
        {
            if (reader.TryGetInt32(out int i))
                return i;
            else if (reader.TryGetInt64(out long l))
                return l;
            else if (reader.TryGetDouble(out double d))
            {
                // the idea is to see if there is a loss of accuracy of value

                // example:
                // value from json       : 636474637870330463636474637870330463636474637870330463
                // successfully parsed d : 6.3647463787033043E+53
                // new BigInteger(d)     : 636474637870330432588044308848152942336795574685138944
                if (JsonConverterBigInteger.TryGetBigInteger(ref reader, out var bi))
                {
                    if (bi != new BigInteger(d))
                        return bi;
                }
                return d;
            }
            else if (reader.TryGetDecimal(out decimal dm))
            {
                // the same idea as for a double value above
                if (JsonConverterBigInteger.TryGetBigInteger(ref reader, out var bi))
                {
                    if (bi != new BigInteger(dm))
                        return bi;
                }
                return dm;
            }

            var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
#if NETSTANDARD2_0
            var data = span.ToArray();
#else
            var data = span;
#endif

            throw new NotImplementedException($"Unexpected Number value. Raw text was: {Encoding.UTF8.GetString(data)}");
        }
    }
}
