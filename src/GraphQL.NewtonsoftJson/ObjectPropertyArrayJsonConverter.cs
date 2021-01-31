using System;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// Converts an instance of <see cref="ObjectProperty"/> array to JSON. Doesn't support read from JSON.
    /// <br/><br/>
    /// Array of <see cref="ObjectProperty"/> is an analogy of Dictionary(string, object) which is naturally
    /// handled by JSON.NET as an object. To write an array as a single object and not an array, this converter
    /// is required. Working through arrays rather than dictionaries saves memory in the managed heap.
    /// </summary>
    public class ObjectPropertyArrayJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType == typeof(ObjectProperty[]);

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            var properties = (ObjectProperty[])value;

            foreach (var property in properties)
            {
                writer.WritePropertyName(property.Key);
                serializer.Serialize(writer, property.Value);
            }

            writer.WriteEndObject();
        }
    }
}
