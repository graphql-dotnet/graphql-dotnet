using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class TimeSpanGraphType : ScalarGraphType
    {
        public TimeSpanGraphType()
        {
            Name = "TimeSpan";
            Description =
                "The `TimeSpan` scalar type represents a period of time. `TimeSpan` expects " +
                "to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        public override object Serialize(object value)
        {
            return ParseValue(value);
        }

        public override object ParseValue(object value)
        {
            return ValueConverter.ConvertTo(value, typeof(TimeSpan));
        }

        public override object ParseLiteral(IValue value)
        {
            if (value is TimeSpanValue)
            {
                return ParseValue(((TimeSpanValue)value).Value);
            }

            if (value is StringValue)
            {
                return ParseValue(((StringValue)value).Value);
            }

            return null;
        }
    }
}
