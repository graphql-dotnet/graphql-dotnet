namespace GraphQL.Types
{
    public abstract class ScalarGraphType : GraphType
    {
        public static StringGraphType String = new StringGraphType();
        public static BooleanGraphType Boolean = new BooleanGraphType();
        public static IntGraphType Int = new IntGraphType();
        public static FloatGraphType Float = new FloatGraphType();
        public static IdGraphType Id = new IdGraphType();

        public abstract object Coerce(object value);
    }
}
