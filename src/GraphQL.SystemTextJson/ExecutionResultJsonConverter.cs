using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphQL.SystemTextJson
{
    public class ExecutionResultJsonConverter : JsonConverter<ExecutionResult>
    {
        public override void Write(Utf8JsonWriter writer, ExecutionResult value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Important: Don't pass the same options down when recursively calling Serialize.
            // See docs: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to
            WriteData(writer, value);
            WriteErrors(writer, value.Errors, value.ExposeExceptions);
            WriteExtensions(writer, value);

            writer.WriteEndObject();
        }

        private void WriteData(Utf8JsonWriter writer, ExecutionResult result)
        {
            var data = result.Data;

            if (result.Errors?.Count > 0 && data == null)
            {
                return;
            }

            WriteDataRecursively(writer, "data", data);
        }

        private void WriteDataRecursively(Utf8JsonWriter writer, string name, object data)
        {
            if (name != null)
            {
                if (data == null)
                {
                    writer.WriteNull(name);
                    return;
                }

                if (data is string s)
                {
                    writer.WriteString(name, s);
                    return;
                }

                if (data is bool b)
                {
                    writer.WriteBoolean(name, b);
                    return;
                }

                writer.WritePropertyName(name);
            }

            if (data is Dictionary<string, object> dictionary)
            {
                writer.WriteStartObject();

                foreach (var kvp in dictionary)
                    WriteDataRecursively(writer, kvp.Key, kvp.Value);

                writer.WriteEndObject();
            }
            else if (data is List<object> list)
            {
                writer.WriteStartArray();

                foreach (object item in list)
                {
                    if (item is null)
                        writer.WriteNullValue();
                    else if (item is string s)
                        writer.WriteStringValue(s);
                    else
                        WriteDataRecursively(writer, null, item);
                }

                writer.WriteEndArray();
            }
            else
            {
                // Need to avoid this call by all means! The question remains open - why this API so expensive?
                JsonSerializer.Serialize(writer, data);
            }
        }

        private void WriteErrors(Utf8JsonWriter writer, ExecutionErrors errors, bool exposeExceptions)
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

                // Check if return StackTrace, including all inner exceptions
                JsonSerializer.Serialize(writer, exposeExceptions ? error.ToString() : error.Message);

                if (error.Locations != null)
                {
                    writer.WritePropertyName("locations");
                    writer.WriteStartArray();
                    error.Locations.Apply(location =>
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("line");
                        JsonSerializer.Serialize(writer, location.Line);
                        writer.WritePropertyName("column");
                        JsonSerializer.Serialize(writer, location.Column);
                        writer.WriteEndObject();
                    });
                    writer.WriteEndArray();
                }

                if (error.Path != null && error.Path.Any())
                {
                    writer.WritePropertyName("path");
                    JsonSerializer.Serialize(writer, error.Path);
                }

                WriteErrorExtensions(writer, error);

                writer.WriteEndObject();
            });

            writer.WriteEndArray();
        }

        private void WriteErrorExtensions(Utf8JsonWriter writer, ExecutionError error)
        {
            if (string.IsNullOrWhiteSpace(error.Code) && (error.Data == null || error.Data.Count == 0))
            {
                return;
            }

            writer.WritePropertyName("extensions");
            writer.WriteStartObject();

            if (!string.IsNullOrWhiteSpace(error.Code))
            {
                writer.WritePropertyName("code");
                JsonSerializer.Serialize(writer, error.Code);
            }

            if (error.HasCodes)
            {
                writer.WritePropertyName("codes");
                writer.WriteStartArray();
                error.Codes.Apply(code => JsonSerializer.Serialize(writer, code));
                writer.WriteEndArray();
            }

            if (error.Data?.Count > 0)
            {
                writer.WritePropertyName("data");
                writer.WriteStartObject();
                error.DataAsDictionary.Apply(entry =>
                {
                    writer.WritePropertyName(entry.Key);
                    JsonSerializer.Serialize(writer, entry.Value);
                });
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        private void WriteExtensions(Utf8JsonWriter writer, ExecutionResult result)
        {
            if (result.Extensions?.Count > 0)
            {
                writer.WritePropertyName("extensions");
                JsonSerializer.Serialize(writer, result.Extensions);
            }
        }

        public override ExecutionResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}
