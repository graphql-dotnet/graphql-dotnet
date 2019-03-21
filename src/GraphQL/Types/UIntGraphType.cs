using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class UIntGraphType : ScalarGraphType
    {
        public UIntGraphType() => Name = "UInt";

        public override object ParseLiteral(IValue value) => (value as UIntValue)?.Value;

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(uint));

        public override object Serialize(object value) => ParseValue(value);
    }
}
