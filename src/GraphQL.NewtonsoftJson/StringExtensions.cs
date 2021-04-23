using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.NewtonsoftJson
{
    /// <summary>
    /// Provides extension methods to deserialize json strings into object dictionaries.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly JsonSerializer _jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new InputsConverter()
            },
        });

        /// <summary>
        /// Converts a JSON-formatted string into a dictionary.
        /// </summary>
        /// <param name="json">A JSON-formatted string.</param>
        /// <returns>Inputs.</returns>
        public static Inputs ToInputs(this string json)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var dictionary = json?.ToDictionary();
#pragma warning restore CS0618 // Type or member is obsolete
            return dictionary.ToInputs();
        }

        /// <summary>
        /// Converts a JSON object into a dictionary.
        /// </summary>
        /// <remarks>
        /// Used by GraphQL.Transports.AspNetCore.NewtonsoftJson project in server repo.
        /// </remarks>
        public static Inputs ToInputs(this JObject obj)
        {
            var variables = obj?.GetValueInternal() as Dictionary<string, object>;
            return variables.ToInputs();
        }

        /// <summary>
        /// Converts a JSON-formatted string into a dictionary.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>Returns a <c>null</c> if the object cannot be converted into a dictionary.</returns>
        [Obsolete("This method will be removed in a future version of GraphQL.NET. Please use the ToInputs method instead.")]
        public static Dictionary<string, object> ToDictionary(this string json)
        {
            if (json == null)
                return null;

            using var stringReader = new System.IO.StringReader(json);
            using var jsonTextReader = new JsonTextReader(stringReader);
            var values = _jsonSerializer.Deserialize(jsonTextReader);
            return GetValueInternal(values) as Dictionary<string, object>;
        }

        /// <summary>
        /// Deserializes a JSON-formatted string of data into the specified type.
        /// Any <see cref="Inputs"/> objects will be deserialized into the proper format.
        /// Property names are matched based on a case insensitive comparison (the default for Newtonsoft.Json).
        /// </summary>
        public static T FromJson<T>(this string json)
        {
            using var stringReader = new System.IO.StringReader(json);
            using var jsonTextReader = new JsonTextReader(stringReader);
            return _jsonSerializer.Deserialize<T>(jsonTextReader);
        }

        /// <summary>
        /// Deserializes a JSON-formatted stream of data into the specified type.
        /// Any <see cref="Inputs"/> objects will be deserialized into the proper format.
        /// Property names are matched based on a case insensitive comparison (the default for Newtonsoft.Json).
        /// </summary>
        public static T FromJson<T>(this System.IO.Stream stream)
        {
            using var streamReader = new System.IO.StreamReader(stream ?? throw new ArgumentNullException(nameof(stream)), System.Text.Encoding.UTF8, false, 1024, true);
            using var jsonTextReader = new JsonTextReader(streamReader);
            return _jsonSerializer.Deserialize<T>(jsonTextReader);
        }

        /// <summary>
        /// Gets the value contained in a JObject, JValue, JProperty or JArray.
        /// </summary>
        /// <param name="value">The object containing the value to extract.</param>
        /// <remarks>If the value is a recognized type, it is returned unaltered.</remarks>
        [Obsolete("This method will be removed in a future version of GraphQL.NET.")]
        public static object GetValue(this object value)
            => GetValueInternal(value);

        private static object GetValueInternal(this object value)
        {
            if (value is JObject objectValue)
            {
                var output = new Dictionary<string, object>();
                foreach (var kvp in objectValue)
                {
                    output.Add(kvp.Key, GetValueInternal(kvp.Value));
                }
                return output;
            }

            if (value is JProperty propertyValue)
            {
                return new Dictionary<string, object>
                {
                    { propertyValue.Name, GetValueInternal(propertyValue.Value) }
                };
            }

            if (value is JArray arrayValue)
            {
                return arrayValue.Children().Aggregate(new List<object>(), (list, token) =>
                {
                    list.Add(GetValueInternal(token));
                    return list;
                });
            }

            if (value is JValue rawValue)
            {
                object val = rawValue.Value;
                if (val is long l)
                {
                    if (l >= int.MinValue && l <= int.MaxValue)
                    {
                        return (int)l;
                    }
                }
                return val;
            }

            return value;
        }
    }
}
