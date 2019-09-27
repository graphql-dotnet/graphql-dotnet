using System;

namespace GraphQL.Conversion
{
    public interface IFieldNameConverter
    {
        string NameFor(string field, Type parentType);
    }
}
