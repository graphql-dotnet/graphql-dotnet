using System;
using System.Collections.Generic;
using GraphQL.Execution;

namespace GraphQL.Tests
{
    internal static class TestExtensions
    {
        public static IReadOnlyDictionary<string, object> ToDict(this object data)
        {
            if (data == null)
                return new Dictionary<string, object>();

            if (data is ObjectExecutionNode objectExecutionNode)
                return (IReadOnlyDictionary<string, object>)objectExecutionNode.ToValue();

            if (data is IReadOnlyDictionary<string, object> properties)
            {
                return properties;
            }

            throw new ArgumentException($"Unknown type {data.GetType()}. Parameter must be of type ObjectExecutionNode or IDictionary<string, object>.", nameof(data));
        }
    }
}
