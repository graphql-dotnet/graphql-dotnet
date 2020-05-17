using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ULongGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            ULongValue ulongValue => ulongValue.Value,
            IntValue intValue => intValue.Value >= 0 ? (ulong?)intValue.Value : null,
            LongValue longValue => longValue.Value >= 0 ? (ulong?)longValue.Value : null,
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(ulong));
    }
}
