using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ByteGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            ByteValue byteValue => byteValue.Value,
            IntValue intValue => byte.MinValue <= intValue.Value && intValue.Value <= byte.MaxValue ? (byte?)intValue.Value : null,
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(byte));
    }
}
