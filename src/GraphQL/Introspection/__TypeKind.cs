using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __TypeKind : EnumerationGraphType
    {
        public __TypeKind()
        {
            Name = "__TypeKind";
            Description = "An enum describing what kind of type a given __Type is.";
            AddValue("SCALAR", "Indicates this type is a scalar.", TypeKind.SCALAR);
            AddValue("OBJECT", "Indicates this type is an object.  `fields` and `possibletypes` are valid fields.", TypeKind.OBJECT);
            AddValue("INTERFACE", "Indicates this type is an interface.  `fields` and `possibleTypes` are valid fields.", TypeKind.INTERFACE);
            AddValue("UNION", "Indicates this type is a union.  `possibleTypes` is a valid field.", TypeKind.UNION);
            AddValue("ENUM", "Indicates this type is an num.  `enumValues` is a valid field.", TypeKind.ENUM);
            AddValue("INPUT_OBJECT", "Indicates this type is an input object.  `inputFields` is a valid field.", TypeKind.INPUT_OBJECT);
            AddValue("LIST", "Indicates this type is a list.  `ofType` is a valid field.", TypeKind.LIST);
            AddValue("NON_NULL", "Indicates this type is a non-null.  `ofType` is a valid field.", TypeKind.NON_NULL);
        }
    }
}