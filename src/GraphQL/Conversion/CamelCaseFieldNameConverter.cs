using System;

namespace GraphQL.Conversion
{
    public class CamelCaseFieldNameConverter : IFieldNameConverter
    {
        public static readonly CamelCaseFieldNameConverter Instance = new CamelCaseFieldNameConverter();

        public string NameFor(string field, Type parentType) => field.ToCamelCase();
    }
}
