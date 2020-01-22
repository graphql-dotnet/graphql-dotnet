using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GraphQL.SystemTextJson
{
    public static class StringExtensions
    {
        public static JsonElement ToVariables(this string json)
        {
            using var jsonDoc = JsonDocument.Parse(json);
            return jsonDoc.RootElement.Clone();
        }

        /// <summary>
        /// Converts a JSON-formatted string into a dictionary.
        /// </summary>
        /// <param name="json">A JSON formatted string.</param>
        /// <returns>Inputs.</returns>
        public static Inputs ToInputs(this string json)
        {
            var dictionary = json != null ? ToDictionary(json) : null;
            return dictionary == null
                ? new Inputs()
                : new Inputs(dictionary);
        }

        /// <summary>
        /// Converts a JSON object into a dictionary.
        /// </summary>
        public static Inputs ToInputs(this JsonElement obj)
        {
            var variables = obj.GetValue() as Dictionary<string, object>
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
            using var jsonDoc = JsonDocument.Parse(json);
            var value = jsonDoc.RootElement;
            return GetValue(value) as Dictionary<string, object>;
        }

        /// <summary>
        /// Gets the value contained in a JsonElement.
        /// </summary>
        /// <param name="jsonElement">The object containing the value to extract.</param>
        /// <remarks>If the value is a recognized type, it is returned unaltered.</remarks>
        private static object GetValue(this JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Object:
                    var output = new Dictionary<string, object>();
                    foreach (var kvp in jsonElement.EnumerateObject())
                    {
                        output.Add(kvp.Name, GetValue(kvp.Value));
                    }
                    return output;
                case JsonValueKind.Array:
                    return jsonElement
                        .EnumerateArray()
                        .Aggregate(new List<object>(), (list, element) =>
                        {
                            list.Add(GetValue(element));
                            return list;
                        });
                case JsonValueKind.Number:
                    // TODO: Extend to other number types...
                    //    var val = rawValue.Value;
                    //    if (val is long l)
                    //    {
                    //        if (l >= int.MinValue && l <= int.MaxValue)
                    //        {
                    //            return (int)l;
                    //        }
                    //    }
                    return jsonElement.GetInt16();
                case JsonValueKind.String:
                    return jsonElement.GetString();
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    throw new NotImplementedException("Unknown JsonValueKind.");
            }
        }
    }
}
