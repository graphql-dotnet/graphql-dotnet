using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class ShortGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            ShortValue shortValue => shortValue.Value,
            IntValue intValue => short.MinValue <= intValue.Value && intValue.Value <= short.MaxValue ? (short?)intValue.Value : null,
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(short));

        public override object Serialize(object value) => ParseValue(value);
    }
}
