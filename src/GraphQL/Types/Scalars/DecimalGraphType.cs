using System;
using System.Globalization;
using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Decimal scalar graph type represents a decimal value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="decimal"/> .NET values to this scalar graph type.
    /// </summary>
    public class DecimalGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value)
        {
            try
            {
                return value switch
                {
                    DecimalValue _ => true,
                    IntValue _ => true,
                    LongValue _ => true,
                    StringValue s => decimal.TryParse(s.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out _),
                    FloatValue f => Ret(checked((decimal)f.Value)),
                    BigIntValue b => Ret(checked((decimal)b.Value)),
                    _ => false
                };
            }
            catch
            {
                return false;
            }

            static bool Ret(decimal _) => true;
        }

        /// <inheritdoc/>
        public override bool CanParseValue(object value)
        {
            try
            {
                return value switch
                {
                    decimal _ => true,
                    int _ => true,
                    long _ => true,
                    string s => decimal.TryParse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out _),
                    float f => Ret(checked((decimal)f)),
                    BigInteger b => Ret(checked((decimal)b)),
                    double d => Ret(checked((decimal)d)),
                    _ => false
                };
            }
            catch
            {
                return false;
            }

            static bool Ret(decimal _) => true;
        }

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            DecimalValue d => d.Value,
            IntValue i => (decimal)i.Value,
            LongValue l => (decimal)l.Value,
            StringValue s => decimal.Parse(s.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo),
            FloatValue f => checked((decimal)f.Value),
            BigIntValue b => checked((decimal)b.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            decimal _ => value, // no boxing
            int i => (decimal)i,
            long l => (decimal)l,
            string s => decimal.Parse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo),
            float f => checked((decimal)f),
            BigInteger b => checked((decimal)b),
            double d => checked((decimal)d),
            _ => null
        };

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new DecimalValue(Convert.ToDecimal(value));
    }
}
