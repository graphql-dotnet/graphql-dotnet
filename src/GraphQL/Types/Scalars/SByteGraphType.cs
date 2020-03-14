using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class SByteGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            SByteValue sbyteValue => sbyteValue.Value,
            IntValue intValue => sbyte.MinValue <= intValue.Value && intValue.Value <= sbyte.MaxValue ? (sbyte?)intValue.Value : null,
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(sbyte));
    }
}
