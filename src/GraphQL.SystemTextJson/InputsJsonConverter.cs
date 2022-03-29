using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// A custom JsonConverter for reading an <see cref="Inputs"/> object.
    /// Unnecessary for writing, as <see cref="Inputs"/> implements
    /// <see cref="IReadOnlyDictionary{TKey, TValue}">IReadOnlyDictionary&lt;string, object&gt;</see>.
    /// </summary>
    public class InputsJsonConverter : JsonConverter<Inputs>
    {
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Inputs value, JsonSerializerOptions options)
            => JsonSerializer.Serialize<IReadOnlyDictionary<string, object?>>(writer, value, options);

        /// <inheritdoc/>
        public override Inputs Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ReadDictionary(ref reader).ToInputs();

        private static Dictionary<string, object?> ReadDictionary(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var result = new Dictionary<string, object?>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                string key = reader.GetString()!;

                // move to property value
                if (!reader.Read())
                    throw new JsonException();

                result.Add(key, ReadValue(ref reader));
            }

            return result;
        }

        private static object? ReadValue(ref Utf8JsonReader reader)
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

        private static List<object?> ReadArray(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            var result = new List<object?>();

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
            {
                return i;
            }
            else if (reader.TryGetInt64(out long l))
            {
                return l;
            }
            else if (JsonConverterBigInteger.TryGetBigInteger(ref reader, out var bi))
            {
                return bi;
            }
            else
            {
                bool isDouble = reader.TryGetDouble(out double dbl);
                bool isDecimal = reader.TryGetDecimal(out decimal dec);

                if (isDouble && !isDecimal)
                    return dbl;

                if (!isDouble && isDecimal)
                    return dec;

                if (isDouble && isDecimal)
                {
                    // Cast the decimal to our struct to avoid the decimal.GetBits allocations.
                    var decBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref dec);
                    decimal temp = new(dbl);
                    var dblAsDecBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref temp);
                    return decBits.Equals(dblAsDecBits)
                        ? dbl
                        : dec;
                }
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
