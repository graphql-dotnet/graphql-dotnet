using System;
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
            IntValue intVal => (double)intVal.Value,
            LongValue longVal => (double)longVal.Value,
            DecimalValue decVal => checked((double)decVal.Value),
            BigIntValue bigIntVal => checked((double)bigIntVal.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(double));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value)
        {
            try
            {
                return value switch
                {
                    FloatValue _ => true,
                    IntValue _ => true,
                    LongValue _ => true,
                    DecimalValue decVal => Ret(checked((double)decVal.Value)),
                    BigIntValue bigIntVal => Ret(checked((double)bigIntVal.Value)),
                    _ => false
                };
            }
            catch
            {
                return false;
            }

            static bool Ret(double _) => true;
        }

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new FloatValue(Convert.ToDouble(value));
    }
}
