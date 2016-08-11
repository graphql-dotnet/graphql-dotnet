using GraphQL.Types;

namespace GraphQL.SchemaGenerator.Wrappers
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
