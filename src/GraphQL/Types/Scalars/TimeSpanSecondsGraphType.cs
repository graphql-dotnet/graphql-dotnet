using System;
using GraphQL.Language.AST;

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
                "The `Seconds` scalar type represents a period of time represented as the total number of seconds.";
        }

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => TimeSpan.FromSeconds(intValue.Value),
            LongValue longValue => TimeSpan.FromSeconds(longValue.Value),
            BigIntValue bigIntValue => TimeSpan.FromSeconds((double)bigIntValue.Value),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            TimeSpan _ => value, // no boxing
            int i => TimeSpan.FromSeconds(i),
            long l => TimeSpan.FromSeconds(l),
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object Serialize(object value) => value switch
        {
            TimeSpan timeSpan => (long)timeSpan.TotalSeconds,
            int i => (long)i,
            long _ => value,
            null => null,
            _ => ThrowSerializationError(value)
        };
    }
}
