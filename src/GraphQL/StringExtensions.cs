using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL
{
    public static class StringExtensions
    {
        public static string ToFormat(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static Inputs ToInputs(this string json)
        {
            return new Inputs(ToDictionary(json));
        }

        public static Dictionary<string, object> ToDictionary(this string json)
        {
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var values2 = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> d in values)
            {
                var value = GetValue(d.Value);
                values2.Add(d.Key, value);
            }
            return values2;
        }

        public static object GetValue(object value)
        {
            if (value is JObject)
            {
                return ToDictionary(value.ToString());
            }

            if (value is JArray)
            {
                var array = (JArray) value;
                return array.Values().Aggregate(new List<object>(), (list, token) =>
                {
                    list.Add(GetValue(token.ToObject<object>()));
                    return list;
                });
            }

            return value;
        }
    }
}
