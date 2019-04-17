using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class UIntGraphType : ScalarGraphType
    {
        public UIntGraphType() => Name = "UInt";

        public override object ParseLiteral(IValue value)
        {
            switch (value)
            {
                case UIntValue uintValue:
                    return uintValue.Value;

                case IntValue intValue:
                    if (intValue.Value >= 0)
                        return (uint)intValue.Value;
                    return null;

                case LongValue longValue:
                    if (uint.MinValue <= longValue.Value && longValue.Value <= uint.MaxValue)
                        return (uint)longValue.Value;
                    return null;

                default:
                    return null;
            }
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(uint));

        public override object Serialize(object value) => ParseValue(value);
    }
}
