using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public abstract class ScalarGraphType : GraphType
    {
        public virtual object Serialize(object value) => ParseValue(value);

        public abstract object ParseValue(object value);

        public abstract object ParseLiteral(IValue value);
    }
}
