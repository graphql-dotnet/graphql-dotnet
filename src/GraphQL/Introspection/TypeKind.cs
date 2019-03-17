using System.ComponentModel;

namespace GraphQL.Introspection
{
    public enum TypeKind
    {
        [Description("Indicates this type is a scalar.")]
        SCALAR = 0,
        [Description("Indicates this type is an object. `fields` and `possibleTypes` are valid fields.")]
        OBJECT  = 1,
        [Description("Indicates this type is an interface. `fields` and `possibleTypes` are valid fields.")]
        INTERFACE = 2,
        [Description("Indicates this type is a union. `possibleTypes` is a valid field.")]
        UNION = 3,
        [Description("Indicates this type is an enum. `enumValues` is a valid field.")]
        ENUM = 4,
        [Description("Indicates this type is an input object. `inputFields` is a valid field.")]
        INPUT_OBJECT = 5,
        [Description("Indicates this type is a list. `ofType` is a valid field.")]
        LIST = 6,
        [Description("Indicates this type is a non-null. `ofType` is a valid field.")]
        NON_NULL = 7
    }
}
