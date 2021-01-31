using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Tests
{
    internal static class TestExtensions
    {
        public static Dictionary<string, object> ToDict(this object data)
        {
            if (data == null)
                return new Dictionary<string, object>();

            return data is ObjectProperty[] properties
                ? properties.ToDictionary(x => x.Key, x => x.Value)
                : throw new ArgumentException($"Unknown type {data.GetType()}. Parameter must be of type ObjectProperty[].", nameof(data));
        }
    }
}
