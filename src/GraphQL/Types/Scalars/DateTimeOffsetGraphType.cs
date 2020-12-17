using System;
using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// The DateTimeOffset scalar graph type represents a date, time and offset from UTC.
    /// By default <see cref="GraphTypeTypeRegistry"/> maps all <see cref="DateTimeOffset"/> .NET values to this scalar graph type.
    /// </summary>
    public class DateTimeOffsetGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffsetGraphType"/> class.
        /// </summary>
        public DateTimeOffsetGraphType()
        {
            Description =
                "The `DateTimeOffset` scalar type represents a date, time and offset from UTC. `DateTimeOffset` expects timestamps " +
                "to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            DateTimeOffsetValue offsetValue => offsetValue.Value,
            StringValue stringValue => ParseValue(stringValue.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(DateTimeOffset));
    }
}
