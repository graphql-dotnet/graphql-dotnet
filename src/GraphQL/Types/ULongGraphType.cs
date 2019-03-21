using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ULongGraphType : ScalarGraphType
    {
        public ULongGraphType() => Name = "ULong";

        public override object ParseLiteral(IValue value) => (value as ULongValue)?.Value;

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(ulong));

        public override object Serialize(object value) => ParseValue(value);
    }
}
