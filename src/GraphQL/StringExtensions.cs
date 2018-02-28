using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL
{
    public static class StringExtensions
    {
        /// <summary>
        /// Determines whether the specified string is empty.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns><c>true</c> if the specified string is empty; otherwise, <c>false</c>.</returns>
        public static bool IsEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// Splits a string on commas (,)
        /// </summary>
        /// <param name="content">The string to split.</param>
        public static string[] ToDelimitedArray(this string content)
        {
            return content.ToDelimitedArray(',');
        }

        /// <summary>
        /// Splits a string on the indicated character.
        /// </summary>
        /// <param name="content">The string to split.</param>
        /// <param name="delimiter">The delimiter.</param>
        public static string[] ToDelimitedArray(this string content, char delimiter)
        {
            var array = content.Split(delimiter);
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = array[i].Trim();
            }

            return array;
        }

        /// <summary>
        /// Equivalent to String.GetEnumerator.
        /// </summary>
        /// <param name="this">The this.</param>
        public static IEnumerable<char> ToEnumerable(this string @this)
        {
            if (@this == null) throw new ArgumentNullException("@this");

            for (var i = 0; i < @this.Length; ++i)
            {
                yield return @this[i];
            }
        }

        /// <summary>
        /// Converts an enumeration of Char into a string.
        /// </summary>
        /// <param name="chars">The chars.</param>
        /// <returns>System.String.</returns>
        public static string ToStr(this IEnumerable<char> chars)
        {
            return new string(chars.ToArray());
        }

        /// <summary>
        /// Equivalent to String.Format.
        /// </summary>
        /// <param name="format">The format string in String.Format style.</param>
        /// <param name="args">The arguments.</param>
        public static string ToFormat(this string format, params object[] args)
        {
            return string.Format(format, args);
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
        public static Inputs ToInputs(this JObject obj)
        {
            var variables = obj?.GetValue() as Dictionary<string, object>
                            ?? new Dictionary<string, object>();
            return new Inputs(variables);
        }

        /// <summary>
        /// Converts a JSON formatted string into a the dictionary.
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
        /// Returns a camel case version of the string.
        /// </summary>
        public static string ToCamelCase(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            return $"{char.ToLowerInvariant(s[0])}{s.Substring(1)}";
        }

        /// <summary>
        /// Returns a pascal case version of the string.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>System.String.</returns>
        public static string ToPascalCase(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            return $"{char.ToUpperInvariant(s[0])}{s.Substring(1)}";
        }

        /// <summary>
        /// Gets the value contained in a JObject, JValue, JProperty, or JArray.
        /// </summary>
        /// <param name="value">The object containing the value to extract.</param>
        /// <remarks>If the value is a recognized type, it is returned unaltered.</remarks>
        public static object GetValue(this object value)
        {
            var objectValue = value as JObject;
            if (objectValue != null)
            {
                var output = new Dictionary<string, object>();
                foreach (var kvp in objectValue)
                {
                    output.Add(kvp.Key, GetValue(kvp.Value));
                }
                return output;
            }

            var propertyValue = value as JProperty;
            if (propertyValue != null)
            {
                return new Dictionary<string, object>
                {
                    { propertyValue.Name, GetValue(propertyValue.Value) }
                };
            }

            var arrayValue = value as JArray;
            if (arrayValue != null)
            {
                return arrayValue.Children().Aggregate(new List<object>(), (list, token) =>
                {
                    list.Add(GetValue(token));
                    return list;
                });
            }

            var rawValue = value as JValue;
            if (rawValue != null)
            {
                var val = rawValue.Value;
                if (val is long)
                {
                    long l = (long)val;
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
