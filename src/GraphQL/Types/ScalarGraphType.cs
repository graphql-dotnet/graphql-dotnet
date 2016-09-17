using GraphQL.Language;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public abstract class ScalarGraphType : GraphType
    {
        public abstract object Serialize(object value);

        public abstract object ParseValue(object value);

        public abstract object ParseLiteral(IValue value);
    }


    public static class ScalarGraphTypes 
    {
        public static ScalarGraphType String = new StringGraphType();
        public static ScalarGraphType Float = new FloatGraphType();
        public static ScalarGraphType Int = new IntGraphType();
        public static ScalarGraphType Date = new DateGraphType();
        public static ScalarGraphType Boolean = new BooleanGraphType();
        public static ScalarGraphType Id = new IdGraphType();
    }
}
