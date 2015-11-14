using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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
                if (d.Value is JObject)
                {
                    values2.Add(d.Key, ToDictionary(d.Value.ToString()));
                }
                else
                {
                    values2.Add(d.Key, d.Value);
                }
            }
            return values2;
        }
    }
}
