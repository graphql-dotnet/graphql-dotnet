using System;
using System.Linq;
using GraphQL.Execution;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    public class ExecutionResultJsonConverter : JsonConverter
    {
        private readonly IErrorInfoProvider _errorParser;

        public ExecutionResultJsonConverter(IErrorInfoProvider errorParser)
        {
            _errorParser = errorParser;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is ExecutionResult result)
            {
                writer.WriteStartObject();

                WriteErrors(result.Errors, writer, serializer);
                WriteData(result, writer, serializer);
                WriteExtensions(result, writer, serializer);

                writer.WriteEndObject();
            }
        }

        private void WriteData(ExecutionResult result, JsonWriter writer, JsonSerializer serializer)
        {
            var data = result.Data;

            if (result.Errors?.Count > 0 && data == null)
            {
                return;
            }

            writer.WritePropertyName("data");
            serializer.Serialize(writer, data);
        }

        private void WriteErrors(ExecutionErrors errors, JsonWriter writer, JsonSerializer serializer)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            writer.WritePropertyName("errors");

            writer.WriteStartArray();

            errors.Select(error => new { Error = error, Info = _errorParser.GetInfo(error) }).Apply(error =>
            {
                writer.WriteStartObject();

                writer.WritePropertyName("message");

                serializer.Serialize(writer, error.Info.Message);

                if (error.Error.Locations != null)
                {
                    writer.WritePropertyName("locations");
                    writer.WriteStartArray();
                    error.Error.Locations.Apply(location =>
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

                if (error.Error.Path != null && error.Error.Path.Any())
                {
                    writer.WritePropertyName("path");
                    serializer.Serialize(writer, error.Error.Path);
                }

                if (error.Info.Extensions?.Count > 0)
                {
                    writer.WritePropertyName("extensions");
                    serializer.Serialize(writer, error.Info.Extensions);
                } 

                writer.WriteEndObject();
            });

            writer.WriteEndArray();
        }

        private void WriteExtensions(ExecutionResult result, JsonWriter writer, JsonSerializer serializer)
        {
            if (result.Extensions?.Count > 0)
            {
                writer.WritePropertyName("extensions");
                serializer.Serialize(writer, result.Extensions);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(ExecutionResult);
    }
}
