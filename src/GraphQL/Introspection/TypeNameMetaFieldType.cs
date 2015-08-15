using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class TypeNameMetaFieldType : FieldType
    {
        public TypeNameMetaFieldType()
        {
            Name = "__typename";
            Type = typeof(NonNullGraphType<StringGraphType>);
            Description = "The name of the current Object type at runtime.";
            Resolve = (context) => context.ParentType.Name;
        }
    }
}
