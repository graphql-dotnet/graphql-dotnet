using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ULongGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value)
        {
            var ulongValue = value as ULongValue;
            return ulongValue?.Value;
        }

        public override object ParseValue(object value) =>
            ValueConverter.ConvertTo(value, typeof(ulong));

        public override object Serialize(object value) => ParseValue(value);
    }
}
