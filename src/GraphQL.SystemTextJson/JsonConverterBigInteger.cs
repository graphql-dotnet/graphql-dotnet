using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// Json converter for reading and writing <see cref="BigInteger"/> values.
    /// While it is not able to correctly write very large numbers.
    /// </summary>
    public sealed class JsonConverterBigInteger : JsonConverter<BigInteger>
    {
        /// <inheritdoc/>
        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TryGetBigInteger(ref reader, out var bi) ? bi : throw new JsonException();

        /// <summary>
        /// Attempts to read a <see cref="BigInteger"/> value from a <see cref="Utf8JsonReader"/>.
        /// </summary>
#if NETSTANDARD2_0
        public static bool TryGetBigInteger(ref Utf8JsonReader reader, out BigInteger bi)
        {
            var byteArray = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray();
            var str = Encoding.UTF8.GetString(byteArray);
            return BigInteger.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out bi);
        }
#else
        public static bool TryGetBigInteger(ref Utf8JsonReader reader, out BigInteger bi)
        {
            var byteSpan = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            Span<char> chars = stackalloc char[byteSpan.Length];
            Encoding.UTF8.GetChars(reader.ValueSpan, chars);
            return BigInteger.TryParse(chars, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out bi);
        }
#endif

        private static readonly BigInteger _maxBigInteger = (BigInteger)decimal.MaxValue;
        private static readonly BigInteger _minBigInteger = (BigInteger)decimal.MinValue;
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
        {
            if (_minBigInteger <= value && value <= _maxBigInteger)
            {
                writer.WriteNumberValue((decimal)value);
                return;
            }

            // https://stackoverflow.com/questions/64788895/serialising-biginteger-using-system-text-json
            var s = value.ToString(NumberFormatInfo.InvariantInfo);
            using var doc = JsonDocument.Parse(s);
            doc.WriteTo(writer);
        }
    }
}
