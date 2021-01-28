using System;
using System.Collections.Generic;

namespace GraphQL.Utilities
{
    public abstract class SchemaDirectiveVisitor : BaseSchemaNodeVisitor
    {
        public string Name { get; set; }

        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

        public TType GetArgument<TType>(string name, TType defaultValue = default)
        {
            return (TType)GetArgument(typeof(TType), name, defaultValue);
        }

        public object GetArgument(Type argumentType, string name, object defaultValue = null) //TODO: rewrite
        {
            if (Arguments == null || !Arguments.TryGetValue(name, out var arg))
            {
                return defaultValue;
            }

            if (arg is IDictionary<string, object> inputObject)
            {
                var type = argumentType;
                if (type.Namespace?.StartsWith("System", StringComparison.InvariantCulture) == true) //TODO: rewrite
                {
                    return arg;
                }

                return inputObject.ToObject(type);
            }

            return arg.GetPropertyValue(argumentType);
        }
    }
}
