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
            _ => (object)null
        };

        public override object ParseValue(object value) => value switch
        {
            int i => TimeSpan.FromMilliseconds(i),
            long l => TimeSpan.FromMilliseconds(l),
            TimeSpan t => t,
            _ => (object)null
        };

        public override object ParseLiteral(IValue value) => value switch
        {
            TimeSpanValue spanValue => ParseValue(spanValue.Value),
            LongValue longValue => ParseValue(longValue.Value),
            IntValue intValue => ParseValue(intValue.Value),
            _ => null
        };
    }
}
