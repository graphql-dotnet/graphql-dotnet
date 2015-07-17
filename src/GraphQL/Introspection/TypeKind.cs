namespace GraphQL.Introspection
{
    public enum TypeKind
    {
        SCALAR = 0,
        OBJECT  = 1,
        INTERFACE = 2,
        UNION = 3,
        ENUM = 4,
        INPUT_OBJECT = 5,
        LIST = 6,
        NON_NULL = 7
    }
}