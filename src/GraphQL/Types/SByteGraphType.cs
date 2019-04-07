using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class SByteGraphType : ScalarGraphType
    {
        public SByteGraphType() => Name = "SByte";

        public override object ParseLiteral(IValue value)
        {
            switch (value)
            {
                case SByteValue sbyteValue:
                    return sbyteValue.Value;

                case IntValue intValue:
                    if (sbyte.MinValue <= intValue.Value && intValue.Value <= sbyte.MaxValue)
                        return (sbyte)intValue.Value;
                    return null;

                default:
                    return null;
            }
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(sbyte));

        public override object Serialize(object value) => ParseValue(value);
    }
}
