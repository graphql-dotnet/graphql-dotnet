using GraphQL.Types;

namespace GraphQL.SchemaGenerator.Wrappers
{
    public class InterfaceGraphTypeWrapper<T> : InterfaceGraphType, IIgnore
    {
        public InterfaceGraphTypeWrapper()
        {
            ObjectGraphTypeBuilder.Build(this, typeof(T));
        }
    }
}
