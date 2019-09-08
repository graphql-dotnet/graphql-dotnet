using GraphQL.Introspection;
using System;
using System.Linq;

namespace GraphQL.Conversion
{
    public class DefaultFieldNameConverter : IFieldNameConverter
    {
        private static readonly Type[] IntrospectionTypes = { typeof(SchemaIntrospection) };

        public static readonly DefaultFieldNameConverter Instance = new DefaultFieldNameConverter();

        public string NameFor(string field, Type parentType) => isIntrospectionType(parentType) ? field.ToCamelCase() : field;

        private bool isIntrospectionType(Type type) => IntrospectionTypes.Contains(type);
    }
}
