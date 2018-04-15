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

            WriteData(result, writer, serializer);
            WriteErrors(result.Errors, writer, serializer, result.ExposeExceptions);
            WriteExtensions(result, writer, serializer);

            writer.WriteEndObject();
        }

        private void WriteData(ExecutionResult result, JsonWriter writer, JsonSerializer serializer)
        {
            var data = result.Data;

            if (result.Errors?.Any() == true && data == null)
            {
                return;
            }

            writer.WritePropertyName("data");
            serializer.Serialize(writer, data);
        }

        private void WriteErrors(ExecutionErrors errors, JsonWriter writer, JsonSerializer serializer, bool exposeExceptions)
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

                if (!string.IsNullOrWhiteSpace(error.Code))
                {
                    writer.WritePropertyName("code");
                    serializer.Serialize(writer, error.Code);
                }

                if (error.Data != null && error.Data.Count > 0)
                {
                    writer.WritePropertyName("data");
                    writer.WriteStartObject();
                    error.Data.Apply(entry =>
                    {
                        writer.WritePropertyName(entry.Key);
                        serializer.Serialize(writer, entry.Value);
                    });
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            });

            writer.WriteEndArray();
        }

        private void WriteExtensions(ExecutionResult result, JsonWriter writer, JsonSerializer serializer)
        {
            if (result.Data == null || result.Extensions == null || !result.Extensions.Any())
            {
                return;
            }

            writer.WritePropertyName("extensions");
            serializer.Serialize(writer, result.Extensions);
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
