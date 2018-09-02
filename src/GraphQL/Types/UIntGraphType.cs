using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class UIntGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value)
        {
            var uintValue = value as UIntValue;
            return uintValue?.Value;
        }

        public override object ParseValue(object value) =>
            ValueConverter.ConvertTo(value, typeof(uint));

        public override object Serialize(object value) => ParseValue(value);
    }
}
