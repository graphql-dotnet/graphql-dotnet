using GraphQL.Types;

namespace GraphQl.SchemaGenerator.Wrappers
{
    public class ObjectGraphTypeWrapper<T> : ObjectGraphType, IIgnore
    {
        public ObjectGraphTypeWrapper()
        {
            ObjectGraphTypeBuilder.Build(this, typeof(T));
        }
    }
}
