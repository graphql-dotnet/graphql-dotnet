namespace GraphQL.Types
{
    public abstract class ScalarGraphType : GraphType
    {
        public abstract object Coerce(object value);
    }
}
