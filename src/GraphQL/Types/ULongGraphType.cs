using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ULongGraphType : ScalarGraphType
    {
        public ULongGraphType() => Name = "ULong";

        public override object ParseLiteral(IValue value)
        {
            switch (value)
            {
                case ULongValue ulongValue:
                    return ulongValue.Value;

                case IntValue intValue:
                    if (intValue.Value >= 0)
                        return (ulong)intValue.Value;
                    return null;

                case LongValue longValue:
                    if (longValue.Value >= 0)
                        return (ulong)longValue.Value;
                    return null;

                default:
                    return null;
            }
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(ulong));

        public override object Serialize(object value) => ParseValue(value);
    }
}
