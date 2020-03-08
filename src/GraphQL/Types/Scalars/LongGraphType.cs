using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class LongGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            LongValue longValue => longValue.Value,
            IntValue intValue => (long)intValue.Value,
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(long));

        public override object Serialize(object value) => ParseValue(value);
    }
}
