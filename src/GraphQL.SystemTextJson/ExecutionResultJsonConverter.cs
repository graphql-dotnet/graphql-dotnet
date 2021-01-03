using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Execution;

namespace GraphQL.SystemTextJson
{
    public class ExecutionResultJsonConverter : JsonConverter<ExecutionResult>
    {
        private readonly IErrorInfoProvider _errorInfoProvider;

        public ExecutionResultJsonConverter(IErrorInfoProvider errorInfoProvider)
        {
            _errorInfoProvider = errorInfoProvider ?? throw new ArgumentNullException(nameof(errorInfoProvider));
        }

        public override void Write(Utf8JsonWriter writer, ExecutionResult value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Important: Be careful with passing the same options down when recursively calling Serialize.
            // See docs: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to
            WriteErrors(writer, value.Errors, _errorInfoProvider, options);
            WriteData(writer, value, options);
            WriteExtensions(writer, value, options);

            writer.WriteEndObject();
        }

        private static void WriteData(Utf8JsonWriter writer, ExecutionResult result, JsonSerializerOptions options)
        {
            var data = result.Data;

            if (result.Errors?.Count > 0 && data == null)
            {
                return;
            }

            WriteProperty(writer, "data", data, options);
        }

        private static void WriteProperty(Utf8JsonWriter writer, string propertyName, object propertyValue, JsonSerializerOptions options)
        {
            writer.WritePropertyName(propertyName);
            WriteValue(writer, propertyValue, options);
        }

        private static void WriteValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                {
                    writer.WriteNullValue();
                    break;
                }
                case string s:
                {
                    writer.WriteStringValue(s);
                    break;
                }
                case bool b:
                {
                    writer.WriteBooleanValue(b);
                    break;
                }
                case int i:
                {
                    writer.WriteNumberValue(i);
                    break;
                }
                case long l:
                {
                    writer.WriteNumberValue(l);
                    break;
                }
                case float f:
                {
                    writer.WriteNumberValue(f);
                    break;
                }
                case double d:
                {
                    writer.WriteNumberValue(d);
                    break;
                }
                case decimal dm:
                {
                    writer.WriteNumberValue(dm);
                    break;
                }
                case uint ui:
                {
                    writer.WriteNumberValue(ui);
                    break;
                }
                case ulong ul:
                {
                    writer.WriteNumberValue(ul);
                    break;
                }
                case Dictionary<string, object> dictionary:
                {
                    writer.WriteStartObject();

                    foreach (var kvp in dictionary)
                        WriteProperty(writer, kvp.Key, kvp.Value, options);

                    writer.WriteEndObject();

                    break;
                }
                case List<object> list:
                {
                    writer.WriteStartArray();

                    foreach (object item in list)
                        WriteValue(writer, item, options);

                    writer.WriteEndArray();

                    break;
                }
                default:
                {
                    // Need to avoid this call by all means! The question remains open - why this API so expensive?
                    JsonSerializer.Serialize(writer, value, options);
                    break;
                }
            }
        }

        private static void WriteErrors(Utf8JsonWriter writer, ExecutionErrors errors, IErrorInfoProvider errorInfoProvider, JsonSerializerOptions options)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            writer.WritePropertyName("errors");

            writer.WriteStartArray();

            foreach (var error in errors)
            {
                var info = errorInfoProvider.GetInfo(error);

                writer.WriteStartObject();

                writer.WritePropertyName("message");

                JsonSerializer.Serialize(writer, info.Message, options);

                if (error.Locations != null)
                {
                    writer.WritePropertyName("locations");
                    writer.WriteStartArray();
                    error.Locations.Apply(location =>
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("line");
                        JsonSerializer.Serialize(writer, location.Line, options);
                        writer.WritePropertyName("column");
                        JsonSerializer.Serialize(writer, location.Column, options);
                        writer.WriteEndObject();
                    });
                    writer.WriteEndArray();
                }

                if (error.Path != null && error.Path.Any())
                {
                    writer.WritePropertyName("path");
                    JsonSerializer.Serialize(writer, error.Path, options);
                }

                if (info.Extensions?.Count > 0)
                {
                    writer.WritePropertyName("extensions");
                    JsonSerializer.Serialize(writer, info.Extensions);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static void WriteExtensions(Utf8JsonWriter writer, ExecutionResult result, JsonSerializerOptions options)
        {
            if (result.Extensions?.Count > 0)
            {
                writer.WritePropertyName("extensions");
                JsonSerializer.Serialize(writer, result.Extensions, options);
            }
        }

        public override ExecutionResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotImplementedException();

        public override bool CanConvert(Type typeToConvert) => typeof(ExecutionResult).IsAssignableFrom(typeToConvert);
    }
}
