using System;
using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// The Seconds scalar graph type represents a period of time represented as an integer value of the total number of seconds.
    /// By default <see cref="GraphTypeTypeRegistry"/> maps all <see cref="TimeSpan"/> .NET values to this scalar graph type.
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
        public override object Serialize(object value) => value switch
        {
            TimeSpan timeSpan => (long)timeSpan.TotalSeconds,
            int i => i,
            long l => l,
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            TimeSpanValue spanValue => spanValue.Value,
            IntValue intValue => TimeSpan.FromSeconds(intValue.Value),
            LongValue longValue => TimeSpan.FromSeconds(longValue.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            TimeSpan t => t,
            int i => TimeSpan.FromSeconds(i),
            long l => TimeSpan.FromSeconds(l),
            _ => null
        };
    }
}
