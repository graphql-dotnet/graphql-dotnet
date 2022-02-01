using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Seconds scalar graph type represents a period of time represented as an integer value of the total number of seconds.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="TimeSpan"/> .NET values to this scalar graph type.
    /// </summary>
    public class TimeSpanSecondsGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanSecondsGraphType"/> class.
        /// </summary>
        public TimeSpanSecondsGraphType()
        {
            Name = "Seconds";
            Description =
                "The `Seconds` scalar type represents a period of time represented as the total number of seconds in range [-922337203685, 922337203685].";
        }

        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            // TimeSpan stores the time as a long in ticks - 1/10000 of a millisecond. So the second value must be representable in a long.
            GraphQLIntValue v when Long.TryParse(v.Value, out long l) => TimeSpan.FromSeconds(l),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            TimeSpan _ => value, // no boxing
            int i => TimeSpan.FromSeconds(i),
            long l => TimeSpan.FromSeconds(l),
            null => null,
            sbyte sb => TimeSpan.FromSeconds(sb),
            byte b => TimeSpan.FromSeconds(b),
            short s => TimeSpan.FromSeconds(s),
            ushort us => TimeSpan.FromSeconds(us),
            uint ui => TimeSpan.FromSeconds(ui),
            ulong ul => TimeSpan.FromSeconds(ul),
            BigInteger bi => TimeSpan.FromSeconds(checked((double)bi)),
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object? Serialize(object? value) => value switch
        {
            TimeSpan timeSpan => (long)timeSpan.TotalSeconds,
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
