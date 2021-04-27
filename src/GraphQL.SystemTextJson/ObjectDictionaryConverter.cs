using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// A custom JsonConverter for reading a dictionary of objects of their real underlying type.
    /// Doesn't support write.
    /// </summary>
    [Obsolete("This class will be removed in a future version of GraphQL.NET. Please use the InputsConverter instead.")]
    public class ObjectDictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
            => throw new NotImplementedException(
                "This converter currently is only intended to be used to read a JSON object into a strongly-typed representation.");

        /// <inheritdoc/>
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => InputsConverter.ReadDictionary(ref reader);
    }
}
