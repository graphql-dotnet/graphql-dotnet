using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.NewtonsoftJson
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a JSON-formatted string into a dictionary.
        /// </summary>
        /// <param name="json">A JSON-formatted string.</param>
        /// <returns>Inputs.</returns>
        public static Inputs ToInputs(this string json)
            => json?.ToDictionary().ToInputs();

        /// <summary>
        /// Converts a JSON object into a dictionary.
        /// </summary>
        public static Inputs ToInputs(this JObject obj)
        {
            var variables = obj?.GetValue() as Dictionary<string, object>
                ?? new Dictionary<string, object>();
            return new Inputs(variables);
        }

        /// <summary>
        /// Converts a JSON formatted string into a dictionary.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>Returns a <c>null</c> if the object cannot be converted into a dictionary.</returns>
        public static Dictionary<string, object> ToDictionary(this string json)
        {
            var values = JsonConvert.DeserializeObject(json,
                new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateParseHandling = DateParseHandling.None
                });
            return GetValue(values) as Dictionary<string, object>;
        }

        /// <summary>
        /// Gets the value contained in a JObject, JValue, JProperty or JArray.
        /// </summary>
        /// <param name="value">The object containing the value to extract.</param>
        /// <remarks>If the value is a recognized type, it is returned unaltered.</remarks>
        private static object GetValue(this object value)
        {
            if (value is JObject objectValue)
            {
                var output = new Dictionary<string, object>();
                foreach (var kvp in objectValue)
                {
                    output.Add(kvp.Key, GetValue(kvp.Value));
                }
                return output;
            }

            if (value is JProperty propertyValue)
            {
                return new Dictionary<string, object>
                {
                    { propertyValue.Name, GetValue(propertyValue.Value) }
                };
            }

            if (value is JArray arrayValue)
            {
                return arrayValue.Children().Aggregate(new List<object>(), (list, token) =>
                {
                    list.Add(GetValue(token));
                    return list;
                });
            }

            if (value is JValue rawValue)
            {
                var val = rawValue.Value;
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
