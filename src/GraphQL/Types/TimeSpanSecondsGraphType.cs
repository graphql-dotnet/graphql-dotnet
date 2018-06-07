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
            return ParseValue(value);
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(TimeSpan));

        public override object ParseLiteral(IValue value)
        {
            if (value is TimeSpanValue)
            {
                return ParseValue(((TimeSpanValue)value).Value);
            }

            if (value is LongValue)
            {
                return ParseValue(((LongValue)value).Value);
            }

            if (value is IntValue)
            {
                return ParseValue(((IntValue)value).Value);
            }

            return null;
        }
    }
}
