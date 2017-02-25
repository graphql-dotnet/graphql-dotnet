using System;
using System.Linq;
using GraphQL.Introspection;

namespace GraphQL.Conversion
{
    public interface IFieldNameConverter
    {
        string NameFor(string field, Type parentType);
    }

    public class DefaultFieldNameConverter : IFieldNameConverter
    {
        private static Type[] IntrospectionTypes = new[] { typeof(SchemaIntrospection) };

        public string NameFor(string field, Type parentType)
        {
            if (isIntrospectionType(parentType))
            {
                return field.ToCamelCase();
            }

            return field;
        }

        private bool isIntrospectionType(Type type)
        {
            return IntrospectionTypes.Contains(type);
        }
    }

    public class CamelCaseFieldNameConverter : IFieldNameConverter
    {
        public string NameFor(string field, Type parentType)
        {
            return field.ToCamelCase();
        }
    }

    public class PascalCaseFieldNameConverter : IFieldNameConverter
    {
        private static Type[] IntrospectionTypes = new[] { typeof(SchemaIntrospection) };

        public string NameFor(string field, Type parentType)
        {
            if (isIntrospectionType(parentType))
            {
                return field.ToCamelCase();
            }

            return field.ToPascalCase();
        }

        private bool isIntrospectionType(Type type)
        {
            return IntrospectionTypes.Contains(type);
        }
    }
}
