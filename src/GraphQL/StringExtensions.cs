using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL
{
    public static class StringExtensions
    {
        public static string ToStr(this IEnumerable<char> chars)
        {
            return new string(chars.ToArray());
        }

        public static string ToFormat(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static Inputs ToInputs(this string json)
        {
            var dictionary = json != null ? ToDictionary(json) : null;
            return dictionary == null
                ? new Inputs()
                : new Inputs(dictionary);
        }

        private static Dictionary<string, object> ToDictionary(this string json)
        {
            var values = JsonConvert.DeserializeObject(json);
            return GetValue(values) as Dictionary<string, object>;
        }

        private static object GetValue(object value)
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
                    long l = (long) val;
                    if (l >= int.MinValue && l <= int.MaxValue)
                    {
                        return (int) l;
                    }
                }
                return val;
            }

            return value;
        }
    }
}
