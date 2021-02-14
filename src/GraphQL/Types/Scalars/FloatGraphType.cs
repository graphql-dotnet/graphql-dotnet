using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Float scalar graph type represents an IEEE 754 double-precision floating point value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="double"/> and <see cref="float"/> .NET values to this scalar graph type.
    /// </summary>
    public class FloatGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            FloatValue floatVal => floatVal.Value,
            IntValue intVal => checked((double)intVal.Value),
            LongValue longVal => checked((double)longVal.Value),
            DecimalValue decVal => checked((double)decVal.Value),
            BigIntValue bigIntVal => checked((double)bigIntVal.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(double));
    }
}
