using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    public class AnyScalarGraphType : ScalarGraphType
    {
        public AnyScalarGraphType()
        {
            Name = "_Any";
        }

        public override object ParseLiteral(IValue value) => value.Value;

        public override object ParseValue(object value) => value;
    }
}
