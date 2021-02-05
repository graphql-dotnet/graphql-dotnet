using System;
using System.Linq;
using GraphQL.Execution;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// Converts an instance of <see cref="ExecutionResult"/> to JSON. Doesn't support read from JSON.
    /// </summary>
    public class ExecutionResultJsonConverter : JsonConverter
    {
        private readonly IErrorInfoProvider _errorInfoProvider;

        public ExecutionResultJsonConverter(IErrorInfoProvider errorInfoProvider)
        {
            _errorInfoProvider = errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var result = (ExecutionResult)value;

            writer.WriteStartObject();

            WriteErrors(result.Errors, writer, serializer);
            WriteData(result, writer, serializer);
            WriteExtensions(result, writer, serializer);

            writer.WriteEndObject();
        }

        private void WriteData(ExecutionResult result, JsonWriter writer, JsonSerializer serializer)
        {
            if (result.Executed)
            {
                writer.WritePropertyName("data");
                serializer.Serialize(writer, result.Data);
            }
        }

        private void WriteErrors(ExecutionErrors errors, JsonWriter writer, JsonSerializer serializer)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            writer.WritePropertyName("errors");

            writer.WriteStartArray();

            foreach (var error in errors)
            {
                var info = _errorInfoProvider.GetInfo(error);

                writer.WriteStartObject();

                writer.WritePropertyName("message");

                serializer.Serialize(writer, info.Message);

                if (error.Locations != null)
                {
                    writer.WritePropertyName("locations");
                    writer.WriteStartArray();
                    foreach (var location in error.Locations)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("line");
                        serializer.Serialize(writer, location.Line);
                        writer.WritePropertyName("column");
                        serializer.Serialize(writer, location.Column);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }

                if (error.Path != null && error.Path.Any())
                {
                    writer.WritePropertyName("path");
                    serializer.Serialize(writer, error.Path);
                }

                if (info.Extensions?.Count > 0)
                {
                    writer.WritePropertyName("extensions");
                    serializer.Serialize(writer, info.Extensions);
                }

                writer.WriteEndObject();
            }

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

        public override bool CanConvert(Type objectType) => typeof(ExecutionResult).IsAssignableFrom(objectType);
    }
}
