using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class BooleanGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => ((value as BooleanValue)?.Value).Boxed();

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(bool));
    }
}
