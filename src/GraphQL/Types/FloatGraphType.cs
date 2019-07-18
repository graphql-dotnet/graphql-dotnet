using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class FloatGraphType : ScalarGraphType
    {
        public FloatGraphType() => Name = "Float";

        public override object Serialize(object value) => ParseValue(value);

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(double));

        public override object ParseLiteral(IValue value)
        {
            if (value is FloatValue floatVal) return floatVal?.Value;

            if (value is IntValue intVal) return intVal.Value;

            var longVal = value as LongValue;
            return longVal?.Value;
        }
    }
}
