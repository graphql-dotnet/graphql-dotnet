using GraphQL.Language.AST;
using System;

namespace GraphQL.Types
{
    public class DateTimeOffsetGraphType : ScalarGraphType
    {
        public DateTimeOffsetGraphType()
        {
            Description =
                "The `DateTimeOffset` scalar type represents a date, time and offset from UTC. `DateTimeOffset` expects timestamps " +
                "to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(DateTimeOffset));

        public override object ParseLiteral(IValue value) => value switch
        {
            DateTimeOffsetValue offsetValue => offsetValue.Value,
            StringValue stringValue => ParseValue(stringValue.Value),
            _ => null
        };
    }
}
