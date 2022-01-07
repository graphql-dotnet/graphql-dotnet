using System.ComponentModel;

namespace GraphQL.Introspection
{
    /// <summary>
    /// An enumeration representing a kind of GraphQL type.
    /// </summary>
    public enum TypeKind
    {
        /// <summary>
        /// Indicates this type is a scalar.
        /// </summary>
        [Description("Indicates this type is a scalar.")]
        SCALAR = 0,

        /// <summary>
        /// Indicates this type is an object. `fields` and `possibleTypes` are valid fields.
        /// </summary>
        [Description("Indicates this type is an object. `fields` and `possibleTypes` are valid fields.")]
        OBJECT = 1,

        /// <summary>
        /// Indicates this type is an interface. `fields` and `possibleTypes` are valid fields.
        /// </summary>
        [Description("Indicates this type is an interface. `fields` and `possibleTypes` are valid fields.")]
        INTERFACE = 2,

        /// <summary>
        /// Indicates this type is a union. `possibleTypes` is a valid field.
        /// </summary>
        [Description("Indicates this type is a union. `possibleTypes` is a valid field.")]
        UNION = 3,

        /// <summary>
        /// Indicates this type is an enum. `enumValues` is a valid field.
        /// </summary>
        [Description("Indicates this type is an enum. `enumValues` is a valid field.")]
        ENUM = 4,

        /// <summary>
        /// Indicates this type is an input object. `inputFields` is a valid field.
        /// </summary>
        [Description("Indicates this type is an input object. `inputFields` is a valid field.")]
        INPUT_OBJECT = 5,

        /// <summary>
        /// Indicates this type is a list. `ofType` is a valid field.
        /// </summary>
        [Description("Indicates this type is a list. `ofType` is a valid field.")]
        LIST = 6,

        /// <summary>
        /// Indicates this type is a non-null. `ofType` is a valid field.
        /// </summary>
        [Description("Indicates this type is a non-null. `ofType` is a valid field.")]
        NON_NULL = 7
    }

    /// <summary>
    /// Shared and already boxed instances of <see cref="TypeKind"/> to avoid further boxing at runtime.
    /// </summary>
    internal static class TypeKindBoxed
    {
        public static readonly object SCALAR = TypeKind.SCALAR;
        public static readonly object OBJECT = TypeKind.OBJECT;
        public static readonly object INTERFACE = TypeKind.INTERFACE;
        public static readonly object UNION = TypeKind.UNION;
        public static readonly object ENUM = TypeKind.ENUM;
        public static readonly object INPUT_OBJECT = TypeKind.INPUT_OBJECT;
        public static readonly object LIST = TypeKind.LIST;
        public static readonly object NON_NULL = TypeKind.NON_NULL;
    }
}
