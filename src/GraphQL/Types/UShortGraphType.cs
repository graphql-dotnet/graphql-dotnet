using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class UShortGraphType : ScalarGraphType
    {
        public UShortGraphType() => Name = "UShort";

        public override object ParseLiteral(IValue value)
        {
            switch (value)
            {
                case UShortValue ushortValue:
                    return ushortValue.Value;

                case IntValue intValue:
                    if (ushort.MinValue <= intValue.Value && intValue.Value <= ushort.MaxValue)
                        return (ushort)intValue.Value;
                    return null;

                default:
                    return null;
            }
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(ushort));

        public override object Serialize(object value) => ParseValue(value);
    }
}
