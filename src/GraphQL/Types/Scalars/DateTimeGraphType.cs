using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The DateTime scalar graph type represents a date and time in accordance with the ISO-8601 standard.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="DateTime"/> .NET values to this scalar graph type.
    /// </summary>
    public class DateTimeGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeGraphType"/> class.
        /// </summary>
        public DateTimeGraphType()
        {
            Description =
                "The `DateTime` scalar type represents a date and time. `DateTime` expects timestamps " +
                "to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value)
        {
            if (value is DateTimeValue timeValue)
            {
                return timeValue.Value;
            }

            if (value is StringValue stringValue)
            {
                return ParseValue(stringValue.Value);
            }

            return null;
        }

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(DateTime));
    }
}
