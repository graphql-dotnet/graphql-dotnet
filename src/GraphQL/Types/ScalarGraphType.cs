using GraphQL.Language;

namespace GraphQL.Types
{
    public abstract class ScalarGraphType : GraphType
    {
        public abstract object ParseValue(object value);

        public abstract object ParseLiteral(IValue value);
    }
}
