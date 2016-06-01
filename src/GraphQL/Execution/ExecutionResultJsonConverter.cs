using System;
using Newtonsoft.Json;

namespace GraphQL
{
    public class ExecutionResultJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is ExecutionResult)
            {
                var result = (ExecutionResult) value;

                writer.WriteStartObject();

                writeData(result, writer, serializer);
                writeErrors(result.Errors, writer, serializer);

                writer.WriteEndObject();
            }
        }

        private void writeData(ExecutionResult result, JsonWriter writer, JsonSerializer serializer)
        {
            var data = result.Data;

            if (result.Errors?.Count > 0 && data == null)
            {
                return;
            }

            if (result.Errors?.Count > 0)
            {
                data = null;
            }

            writer.WritePropertyName("data");
            serializer.Serialize(writer, data);
        }

        private void writeErrors(ExecutionErrors errors, JsonWriter writer, JsonSerializer serializer)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            writer.WritePropertyName("errors");

            writer.WriteStartArray();

            errors.Apply(error =>
            {
                writer.WriteStartObject();

                writer.WritePropertyName("message");
                serializer.Serialize(writer, error.Message);

                if (error.Locations != null)
                {
                    writer.WritePropertyName("locations");
                    serializer.Serialize(writer, error.Locations);
                }

                writer.WriteEndObject();
            });

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ExecutionResult);
        }
    }
}
