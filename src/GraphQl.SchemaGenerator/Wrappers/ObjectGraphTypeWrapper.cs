using GraphQL.Types;

namespace GraphQL.SchemaGenerator.Wrappers
{
    public class ObjectGraphTypeWrapper<T> : ObjectGraphType, IIgnore
    {
        public ObjectGraphTypeWrapper()
        {
            ObjectGraphTypeBuilder.Build(this, typeof(T));
        }
    }
}
