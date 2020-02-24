using System;
using System.Buffers;
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
        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TryGetBigInteger(ref reader, out var bi) ? bi : throw new JsonException();

        public static bool TryGetBigInteger(ref Utf8JsonReader reader, out BigInteger bi)
        {
            var byteSpan = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            Span<char> chars = stackalloc char[byteSpan.Length];
            Encoding.UTF8.GetChars(reader.ValueSpan, chars);
            return BigInteger.TryParse(chars, out bi);
        }

        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
        {
            // TODO: in fact, there will be a loss of accuracy;
            // TODO: there is no (yet) API on Utf8JsonReader that allows you to write JsonTokenType.Number tokens of arbitrary length

            // example:
            // BigInteger 636474637870330463636474637870330463636474637870330463 -> double 6.3647463787033043E+53

            // see Very_Very_Long_Number_Should_Return_As_Is_For_BigInteger and Very__Very_Long_Number_In_Input_Should_Work_For_BigInteger tests
            // tests succeed because the original (expected) string result first parsed to ExecutionResult and then converted back to string,
            // so finally we compare 6.3647463787033043E+53 with 6.3647463787033043E+53, not with original number value 636474637870330463636474637870330463636474637870330463
            writer.WriteNumberValue((double)value);
        }
    }
}
