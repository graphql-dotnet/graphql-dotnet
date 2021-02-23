using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public static RootExecutionNode ToExecutionTree(this IReadOnlyDictionary<string, object> dictionary)
        {
            var root = new RootExecutionNode(null, null)
            {
                SubFields = dictionary.Select(x => CreateExecutionNode(x.Key, x.Value)).ToArray()
            };
            return root;
        }

        private static ExecutionNode CreateExecutionNode(string name, object value)
        {
            if (value is IEnumerable<KeyValuePair<string, object>> dict)
            {
                return new ObjectExecutionNode(null, null, new GraphQL.Language.AST.Field(new GraphQL.Language.AST.NameNode(name), default), null, default)
                {
                    SubFields = dict.Select(x => CreateExecutionNode(x.Key, x.Value)).ToArray(),
                };
            }
            else if (value?.GetType() != typeof(string) && value is IEnumerable list)
            {
                var newList = new List<ExecutionNode>();
                foreach (var item in list)
                {
                    newList.Add(CreateExecutionNode(null, item));
                }
                return new ArrayExecutionNode(null, null, new GraphQL.Language.AST.Field(new GraphQL.Language.AST.NameNode(name), default), null, default)
                {
                    Items = newList,
                };
            }
            else
            {
                return new ValueExecutionNode(null, null, new GraphQL.Language.AST.Field(new GraphQL.Language.AST.NameNode(name), default), null, default)
                {
                    Result = value
                };
            }
        }
    }
}
