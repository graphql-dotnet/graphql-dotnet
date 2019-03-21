using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class UShortGraphType : ScalarGraphType
    {
        public UShortGraphType() => Name = "UShort";

        public override object ParseLiteral(IValue value) => (value as UShortValue)?.Value;

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(ushort));

        public override object Serialize(object value) => ParseValue(value);
    }
}
