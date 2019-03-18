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

        public override object Serialize(object value)
        {
            if (value is TimeSpan timeSpan)
            {
                return (long)timeSpan.TotalMilliseconds;
            }
            else if (value is int i)
            {
                return i;
            }
            else if (value is long l)
            {
                return l;
            }

            return null;
        }

        public override object ParseValue(object value)
        {
            if (value is int i)
            {
                return TimeSpan.FromMilliseconds(i);
            }
            if (value is long l)
            {
                return TimeSpan.FromMilliseconds(l);
            }
            if (value is TimeSpan t)
            {
                return t;
            }

            return null;
        }

        public override object ParseLiteral(IValue value)
        {
            if (value is TimeSpanValue spanValue)
            {
                return spanValue.Value;
            }

            if (value is LongValue longValue)
            {
                return ParseValue(longValue.Value);
            }

            if (value is IntValue intValue)
            {
                return ParseValue(intValue.Value);
            }

            return null;
        }
    }
}
