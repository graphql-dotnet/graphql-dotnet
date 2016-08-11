using GraphQL.SchemaGenerator.Schema;
using GraphQL.Types;

namespace GraphQL.SchemaGenerator.Wrappers
{
    public class EnumerationGraphTypeWrapper<T> : EnumerationGraphType, IIgnore
    {
        public EnumerationGraphTypeWrapper()
        {
            new SchemaBuilder().BuildEnum(this, typeof(T));
        }
    }
}
