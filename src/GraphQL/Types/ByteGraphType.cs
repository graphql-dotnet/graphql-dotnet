using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ByteGraphType : ScalarGraphType
    {
        public ByteGraphType() => Name = "Byte";

        public override object ParseLiteral(IValue value)
        {
            switch (value)
            {
                case ByteValue byteValue:
                    return byteValue.Value;

                case IntValue intValue:
                    if (byte.MinValue <= intValue.Value && intValue.Value <= byte.MaxValue)
                        return (byte)intValue.Value;
                    return null;

                default:
                    return null;
            }
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(byte));

        public override object Serialize(object value) => ParseValue(value);
    }
}
