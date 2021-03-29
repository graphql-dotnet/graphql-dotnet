using System;
using System.Numerics;
using GraphQL.Language.AST;

#nullable enable

namespace GraphQL.Types
{
    /// <summary>
    /// The Milliseconds scalar graph type represents a period of time represented as an integer value of the total number of milliseconds.
    /// </summary>
    public class TimeSpanMillisecondsGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanMillisecondsGraphType"/> class.
        /// </summary>
        public TimeSpanMillisecondsGraphType()
        {
            Name = "Milliseconds";
            Description =
                "The `Milliseconds` scalar type represents a period of time represented as the total number of milliseconds in range [-922337203685477, 922337203685477].";
        }

        /// <inheritdoc/>
        public override object? ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => TimeSpan.FromMilliseconds(intValue.Value),
            LongValue longValue => TimeSpan.FromMilliseconds(longValue.Value),
            BigIntValue bigIntValue => TimeSpan.FromMilliseconds(checked((double)bigIntValue.Value)),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            TimeSpan _ => value, // no boxing
            int i => TimeSpan.FromMilliseconds(i),
            long l => TimeSpan.FromMilliseconds(l),
            null => null,
            sbyte sb => TimeSpan.FromMilliseconds(sb),
            byte b => TimeSpan.FromMilliseconds(b),
            short s => TimeSpan.FromMilliseconds(s),
            ushort us => TimeSpan.FromMilliseconds(us),
            uint ui => TimeSpan.FromMilliseconds(ui),
            ulong ul => TimeSpan.FromMilliseconds(ul),
            BigInteger bi => TimeSpan.FromMilliseconds(checked((double)bi)),
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object? Serialize(object? value) => value switch
        {
            TimeSpan timeSpan => (long)timeSpan.TotalMilliseconds,
            int i => (long)i,
            long _ => value,
            null => null,
            sbyte sb => checked((long)sb),
            byte b => checked((long)b),
            short s => checked((long)s),
            ushort us => checked((long)us),
            uint ui => checked((long)ui),
            ulong ul => checked((long)ul),
            BigInteger bi => checked((long)bi),
            _ => ThrowSerializationError(value)
        };
    }
}
