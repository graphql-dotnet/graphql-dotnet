using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class BooleanGraphType : ScalarGraphType
    {
        public BooleanGraphType() => Name = "Boolean";

        public override object Serialize(object value) => ParseValue(value);

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(bool));

        public override object ParseLiteral(IValue value) => (value as BooleanValue)?.Value;
    }
}
