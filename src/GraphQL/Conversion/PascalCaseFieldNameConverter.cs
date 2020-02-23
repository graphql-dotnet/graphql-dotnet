using GraphQL.Introspection;
using System;
using System.Linq;

namespace GraphQL.Conversion
{
    public class PascalCaseFieldNameConverter : IFieldNameConverter
    {
        private static readonly Type[] IntrospectionTypes = { typeof(SchemaIntrospection) };

        public static readonly PascalCaseFieldNameConverter Instance = new PascalCaseFieldNameConverter();

        public string NameFor(string field, Type parentType) => isIntrospectionType(parentType) ? field.ToCamelCase() : field.ToPascalCase();

        private bool isIntrospectionType(Type type) => IntrospectionTypes.Contains(type);
    }
}
