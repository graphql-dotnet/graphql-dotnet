using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __TypeKind : EnumerationGraphType<TypeKind>
    {
        public __TypeKind()
        {
            SetName(nameof(__TypeKind), validate: false);
            Description = "An enum describing what kind of type a given __Type is.";
        }
    }
}
