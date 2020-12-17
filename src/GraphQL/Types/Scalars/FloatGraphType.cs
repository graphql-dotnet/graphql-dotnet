using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// The Float scalar graph type represents an IEEE 754 double-precision floating point value. It is one of the five built-in scalars.
    /// By default <see cref="GraphTypeTypeRegistry"/> maps all <see cref="double"/> and <see cref="float"/> .NET values to this scalar graph type.
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
