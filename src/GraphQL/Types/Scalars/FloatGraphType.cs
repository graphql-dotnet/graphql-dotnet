using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class FloatGraphType : ScalarGraphType
    {
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(double));

        public override object ParseLiteral(IValue value) => value switch
        {
            FloatValue floatVal => floatVal.Value,
            IntValue intVal => ParseValue(intVal.Value),
            LongValue longVal => ParseValue(longVal.Value),
            DecimalValue decVal => ParseValue(decVal.Value),
            BigIntValue bigIntVal => ParseValue(bigIntVal.Value),
            _ => null
        };
    }
}
