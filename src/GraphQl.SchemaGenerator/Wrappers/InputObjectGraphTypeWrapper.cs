using GraphQL.Types;

namespace GraphQl.SchemaGenerator.Wrappers
{
    public class InputObjectGraphTypeWrapper<T> : InputObjectGraphType, IIgnore
    {
        public InputObjectGraphTypeWrapper()
        {
            ObjectGraphTypeBuilder.Build(this, typeof(T));
            Name = "Input_" + Name;
        }
    }
}
