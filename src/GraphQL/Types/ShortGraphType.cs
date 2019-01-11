using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ShortGraphType : ScalarGraphType
    {
        public ShortGraphType() => Name = "Short";

        public override object ParseLiteral(IValue value)
        {
            var shortValue = value as ShortValue;
            return shortValue?.Value;
        }

        public override object ParseValue(object value) =>
            ValueConverter.ConvertTo(value, typeof(short));

        public override object Serialize(object value) => ParseValue(value);
    }
}
