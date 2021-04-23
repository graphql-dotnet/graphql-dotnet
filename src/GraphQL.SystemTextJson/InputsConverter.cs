using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// A custom JsonConverter for reading an <see cref="Inputs"/> object.
    /// Doesn't support write.
    /// </summary>
    public class InputsConverter : JsonConverter<Inputs>
    {
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Inputs value, JsonSerializerOptions options)
            => throw new NotImplementedException(
                "This converter currently is only intended to be used to read a JSON object into a strongly-typed representation.");

        /// <inheritdoc/>
        public override Inputs Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ObjectDictionaryConverter.ReadDictionary(ref reader).ToInputs();
    }
}
