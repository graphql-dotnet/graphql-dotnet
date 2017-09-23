using System;
using System.Linq;
using Newtonsoft.Json;

namespace GraphQL
{
    public class ExecutionResultJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is ExecutionResult)) return;

            var result = (ExecutionResult) value;

            writer.WriteStartObject();

            writeData(result, writer, serializer);
            writeErrors(result.Errors, writer, serializer, result.ExposeExceptions);

            writer.WriteEndObject();
        }

        private void writeData(ExecutionResult result, JsonWriter writer, JsonSerializer serializer)
        {
            var data = result.Data;

            if (result.Errors?.Any() == true && data == null)
            {
                return;
            }

            writer.WritePropertyName("data");
            serializer.Serialize(writer, data);
        }

        private void writeErrors(ExecutionErrors errors, JsonWriter writer, JsonSerializer serializer, bool exposeExceptions)
        {
            if (errors == null || !errors.Any())
            {
                return;
            }

            writer.WritePropertyName("errors");

            writer.WriteStartArray();

            errors.Apply(error =>
            {
                writer.WriteStartObject();

                writer.WritePropertyName("message");

                // check if return StackTrace, including all inner exceptions
                serializer.Serialize(writer, exposeExceptions ? error.ToString() : error.Message);

                if (error.Locations != null)
                {
                    writer.WritePropertyName("locations");
                    writer.WriteStartArray();
                    error.Locations.Apply(location =>
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("line");
                        serializer.Serialize(writer, location.Line);
                        writer.WritePropertyName("column");
                        serializer.Serialize(writer, location.Column);
                        writer.WriteEndObject();
                    });
                    writer.WriteEndArray();
                }

                if (error.Path != null && error.Path.Any())
                {
                    writer.WritePropertyName("path");
                    serializer.Serialize(writer, error.Path);
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
