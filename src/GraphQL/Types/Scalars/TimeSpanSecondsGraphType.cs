using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class TimeSpanSecondsGraphType : ScalarGraphType
    {
        public TimeSpanSecondsGraphType()
        {
            Name = "Seconds";
            Description =
                "The `Seconds` scalar type represents a period of time represented as the total number of seconds.";
        }

        public override object Serialize(object value) => value switch
        {
            TimeSpan timeSpan => (long)timeSpan.TotalSeconds,
            int i => i,
            long l => l,
            _ => null
        };

        public override object ParseLiteral(IValue value) => value switch
        {
            TimeSpanValue spanValue => spanValue.Value,
            IntValue intValue => TimeSpan.FromSeconds(intValue.Value),
            LongValue longValue => TimeSpan.FromSeconds(longValue.Value),
            _ => null
        };

        public override object ParseValue(object value) => value switch
        {
            TimeSpan t => t,
            int i => TimeSpan.FromSeconds(i),
            long l => TimeSpan.FromSeconds(l),
            _ => null
        };
    }
}
