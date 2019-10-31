using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class LongGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value)
        {
            switch (value)
            {
                case LongValue longValue:
                    return longValue.Value;

                case IntValue intValue:
                    return (long)intValue.Value;

                default:
                    return null;
            }
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(long));

        public override object Serialize(object value) => ParseValue(value);
    }
}
