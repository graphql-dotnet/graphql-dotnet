using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Float scalar graph type represents an IEEE 754 double-precision floating point value.
    /// </summary>
    public class FloatGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(double));

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            FloatValue floatVal => floatVal.Value,
            IntValue intVal => intVal.Value,
            LongValue longVal => longVal.Value,
            _ => null
        };
    }
}
