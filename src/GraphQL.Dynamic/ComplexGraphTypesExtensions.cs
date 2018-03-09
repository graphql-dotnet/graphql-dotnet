using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Dynamic.Types.LiteralGraphType;
using GraphQL.Types;

namespace GraphQL.Dynamic
{
    public static class ComplexGraphTypesExtensions
    {
        public static FieldType RemoteField<TSourceType>(this ComplexGraphType<TSourceType> self, IEnumerable<Type> types, string remoteMoniker, string typeName, string name, string description = null, QueryArguments arguments = null, Func<ResolveFieldContext<TSourceType>, object> resolve = null, string deprecationReason = null)
        {
            var type = types.FirstOrDefault(t => t.GetCustomAttributes(true).Any(a => a is RemoteLiteralGraphTypeMetadataAttribute metadata && metadata.RemoteMoniker == remoteMoniker && metadata.Name == typeName));
            if (type == null)
            {
                throw new ArgumentException($"Couldn't find a type in {nameof(types)} with remote '{remoteMoniker}' and name '{name}'");
            }

            return self.Field(type, name, description, arguments, resolve, deprecationReason);
        }
    }
}
