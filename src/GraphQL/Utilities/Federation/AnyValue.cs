using GraphQL.Language.AST;

namespace GraphQL.Utilities.Federation
{
    public class AnyValue : ValueNode<object>
    {
        public AnyValue(object value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<object> node) => false;
    }
}
