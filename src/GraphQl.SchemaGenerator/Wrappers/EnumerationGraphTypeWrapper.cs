using GraphQl.SchemaGenerator.Schema;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator.Wrappers
{
    public class EnumerationGraphTypeWrapper<T> : EnumerationGraphType, IIgnore
    {
        public EnumerationGraphTypeWrapper()
            : this(null)
        {
        }

        public EnumerationGraphTypeWrapper(ISchema schema)
        {
            new SchemaBuilder().BuildEnum(this, typeof(T));
        }
    }
}
