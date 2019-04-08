using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ShortGraphType : ScalarGraphType
    {
        public ShortGraphType() => Name = "Short";

        public override object ParseLiteral(IValue value)
        {
            switch (value)
            {
                case ShortValue shortValue:
                    return shortValue.Value;

                case IntValue intValue:
                    if (short.MinValue <= intValue.Value && intValue.Value <= short.MaxValue)
                        return (short)intValue.Value;
                    return null;

                default:
                    return null;
            }
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(short));

        public override object Serialize(object value) => ParseValue(value);
    }
}
