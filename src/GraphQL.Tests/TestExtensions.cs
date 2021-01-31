using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Tests
{
    internal static class TestExtensions
    {
        public static IReadOnlyDictionary<string, object> ToDict(this object data)
        {
            if (data == null)
                return new Dictionary<string, object>();

            if (data is IReadOnlyDictionary<string, object> properties)
            {
                var ret = new Dictionary<string, object>();
                foreach (var obj in properties)
                    ret.Add(obj.Key, obj.Value);
                return ret;
            }

            throw new ArgumentException($"Unknown type {data.GetType()}. Parameter must be of type IDictionary<string, object>.", nameof(data));
        }
    }
}
