using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __TypeKind : EnumerationGraphType<TypeKind>
    {
        public __TypeKind()
        {
            Name = nameof(__TypeKind);
            Description = "An enum describing what kind of type a given __Type is.";
        }
    }
}
