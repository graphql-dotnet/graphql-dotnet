using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class UShortGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            UShortValue ushortValue => ushortValue.Value,
            IntValue intValue => ushort.MinValue <= intValue.Value && intValue.Value <= ushort.MaxValue ? (ushort?)intValue.Value : null,
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(ushort));
    }
}
