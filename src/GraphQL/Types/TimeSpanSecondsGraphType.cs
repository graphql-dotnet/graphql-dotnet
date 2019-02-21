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

        public override object Serialize(object value)
        {
            if (value is TimeSpan timeSpan)
            {
                return (long)timeSpan.TotalSeconds;
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

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(TimeSpan));

        public override object ParseLiteral(IValue value)
        {
            if (value is TimeSpanValue spanValue)
            {
                return ParseValue(spanValue.Value);
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
