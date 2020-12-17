using System;
using GraphQL.Language.AST;

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
                "The `Milliseconds` scalar type represents a period of time represented as the total number of milliseconds.";
        }

        /// <inheritdoc/>
        public override object Serialize(object value) => value switch
        {
            TimeSpan timeSpan => (long)timeSpan.TotalMilliseconds,
            int i => i,
            long l => l,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            TimeSpanValue spanValue => spanValue.Value,
            IntValue intValue => TimeSpan.FromMilliseconds(intValue.Value),
            LongValue longValue => TimeSpan.FromMilliseconds(longValue.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            TimeSpan t => t,
            int i => TimeSpan.FromMilliseconds(i),
            long l => TimeSpan.FromMilliseconds(l),
            _ => null
        };
    }
}
