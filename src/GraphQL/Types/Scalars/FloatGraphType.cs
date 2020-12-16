using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class FloatGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            FloatValue floatVal => floatVal.Value,
            IntValue intVal => checked((double)intVal.Value),
            LongValue longVal => checked((double)longVal.Value),
            DecimalValue decVal => checked((double)decVal.Value),
            BigIntValue bigIntVal => checked((double)bigIntVal.Value),
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(double));
    }
}
