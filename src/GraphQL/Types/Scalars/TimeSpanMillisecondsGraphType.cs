using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class TimeSpanMillisecondsGraphType : ScalarGraphType
    {
        public TimeSpanMillisecondsGraphType()
        {
            Name = "Milliseconds";
            Description =
                "The `Milliseconds` scalar type represents a period of time represented as the total number of milliseconds.";
        }

        public override object Serialize(object value) => value switch
        {
            TimeSpan timeSpan => (long)timeSpan.TotalMilliseconds,
            int i => i,
            long l => l,
            _ => null
        };

        public override object ParseLiteral(IValue value) => value switch
        {
            TimeSpanValue spanValue => spanValue.Value,
            IntValue intValue => TimeSpan.FromMilliseconds(intValue.Value),
            LongValue longValue => TimeSpan.FromMilliseconds(longValue.Value),
            _ => null
        };

        public override object ParseValue(object value) => value switch
        {
            TimeSpan t => t,
            int i => TimeSpan.FromMilliseconds(i),
            long l => TimeSpan.FromMilliseconds(l),
            _ => null
        };
    }
}
